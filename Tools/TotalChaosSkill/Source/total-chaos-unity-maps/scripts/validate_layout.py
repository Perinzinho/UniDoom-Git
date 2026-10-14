#!/usr/bin/env python3
"""Valida um layout JSON do Total Chaos Maps antes de ir para a Unity.

Uso: python validate_layout.py caminho/layout.json

Erros (exit code 1) = o mapa quebra ou fica impossível de terminar.
Avisos = problemas de design/pacing que valem uma segunda olhada.
"""
import sys
from collections import Counter, deque

from tc_layout import DOORS, MARKERS, VALID, Grid, load, reachable, walkable


def regions(grid):
    """Rotula regiões (salas/corredores/pátios) separadas por portas."""
    label = {}
    n = 0
    for x, r, ch in grid.cells():
        if not walkable(ch) or ch in DOORS or (x, r) in label:
            continue
        n += 1
        q = deque([(x, r)])
        label[(x, r)] = n
        while q:
            cx, cr = q.popleft()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, nr = cx + dx, cr + dy
                c2 = grid.at(nx, nr)
                if walkable(c2) and c2 not in DOORS and (nx, nr) not in label:
                    label[(nx, nr)] = n
                    q.append((nx, nr))
    return label, n


def main(path):
    data = load(path)
    errors, warns, info = [], [], []

    lines = data.get("grid")
    if not lines or not isinstance(lines, list):
        print("ERRO: campo 'grid' ausente ou não é lista de strings.")
        return 1

    # --- caracteres e larguras ---
    widths = {len(l) for l in lines}
    if len(widths) > 1:
        warns.append(f"Linhas com larguras diferentes {sorted(widths)} (o builder completa com vazio).")
    for r, l in enumerate(lines):
        for x, ch in enumerate(l):
            if ch not in VALID:
                errors.append(f"Caractere inválido {ch!r} em col {x}, linha {r}.")

    shell = data.get("autoShell", True)
    g = Grid(lines, shell)

    # --- vazamentos (só relevante sem autoShell) ---
    if not shell:
        for x, r, ch in g.cells():
            if walkable(ch) and any(g.at(x + dx, r + dy) == " "
                                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                errors.append(f"Célula andável aberta para o vazio em col {x-1}, linha {r-1} (sem autoShell).")
                break

    # --- start / exit ---
    starts, exits = g.find("P"), g.find("X")
    if len(starts) != 1:
        errors.append(f"Esperado exatamente 1 'P' (player start); achei {len(starts)}.")
    if not exits:
        warns.append("Nenhum 'X' (saída/objetivo) no mapa.")

    # --- portas ---
    for x, r, ch in g.cells():
        if ch in DOORS:
            lr = g.at(x - 1, r) == "#" and g.at(x + 1, r) == "#"
            ud = g.at(x, r - 1) == "#" and g.at(x, r + 1) == "#"
            if not (lr or ud):
                warns.append(f"Porta '{ch}' em col {x-1}, linha {r-1} não está numa linha de parede (vai ficar solta).")
            if lr and ud:
                warns.append(f"Porta '{ch}' em col {x-1}, linha {r-1} cercada de paredes nos 4 lados (inacessível).")
        if ch in "CO":
            if any(g.at(x + dx, r + dy) in DOORS for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                warns.append(f"Prop '{ch}' em col {x-1}, linha {r-1} bloqueia uma porta.")

    # --- chaves x portas ---
    doors_locked = {int(ch) for _, _, ch in g.cells() if ch in "123456789"}
    keys_present = {ord(ch) - 96 for _, _, ch in g.cells() if ch in "abcdefghi"}
    for n in sorted(doors_locked - keys_present):
        errors.append(f"Porta trancada '{n}' sem chave correspondente ('{chr(96+n)}') no mapa.")
    for n in sorted(keys_present - doors_locked):
        warns.append(f"Chave '{chr(96+n)}' não abre nenhuma porta ('{n}' não existe).")

    # --- alcançabilidade (simulando o jogador) ---
    seen, got_keys = reachable(g)
    if starts:
        unreachable = [(x, r, ch) for x, r, ch in g.cells()
                       if walkable(ch) and (x, r) not in seen]
        for x, r, ch in unreachable:
            if ch in "XIEabcdefghi":
                what = {"X": "SAÍDA", "I": "item", "E": "inimigo"}.get(ch, f"chave '{ch}'")
                errors.append(f"{what} inalcançável em col {x-1}, linha {r-1} (soft-lock ou ilha isolada).")
        if unreachable and not any("inalcançável" in e for e in errors):
            warns.append(f"{len(unreachable)} células andáveis nunca alcançadas pelo jogador (salas decorativas isoladas?).")

    # --- pacing / métricas ---
    cnt = Counter(ch for _, _, ch in g.cells())
    n_walk = sum(1 for _, _, ch in g.cells() if walkable(ch))
    n_enemy, n_item = cnt["E"], cnt["I"]
    n_light, n_doors = cnt["L"], sum(cnt[d] for d in DOORS)
    info.append(f"Área andável: {n_walk} células | Portas: {n_doors} | Luzes: {n_light}")
    info.append(f"Inimigos: {n_enemy} | Itens: {n_item} | Chaves: {len(keys_present)}")

    if n_enemy and n_item > n_enemy:
        warns.append("Mais itens do que inimigos: pouca escassez para um survival horror.")
    if starts and any(abs(x - starts[0][0]) + abs(r - starts[0][1]) <= 3 for x, r in g.find("E")):
        warns.append("Inimigo a ≤3 células do player start (spawn-kill / susto barato).")
    if n_walk and n_light / n_walk < 0.01:
        warns.append("Muito pouca luz (<1 por 100 células): confira se há pontos de luz/segurança.")

    label, nreg = regions(g)
    if nreg:
        edges, deg = 0, Counter()
        parent = list(range(nreg + 1))

        def find(a):
            while parent[a] != a:
                parent[a] = parent[parent[a]]
                a = parent[a]
            return a

        for x, r, ch in g.cells():
            if ch not in DOORS:
                continue
            nb = {label[(x + dx, r + dy)] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                  if (x + dx, r + dy) in label}
            if len(nb) == 2:
                a, b = tuple(nb)
                edges += 1
                deg[a] += 1
                deg[b] += 1
                parent[find(a)] = find(b)
        comps = len({find(i) for i in range(1, nreg + 1)})
        loops = edges - nreg + comps
        dead_ends = sum(1 for i in range(1, nreg + 1) if deg[i] == 1)
        info.append(f"Regiões: {nreg} | Conexões (portas): {edges} | Loops independentes: {loops} | Becos sem saída: {dead_ends}")
        if nreg >= 4 and loops < 1:
            warns.append("Mapa 100% linear (nenhum loop). Adicione atalhos que reconectem áreas já visitadas.")

        sizes = Counter(label.values())
        light_regions = {label[(x, r)] for x, r, ch in g.cells() if ch == "L" and (x, r) in label}
        dark = [i for i, s in sizes.items() if s >= 12 and i not in light_regions]
        if dark:
            info.append(f"{len(dark)} região(ões) grande(s) sem nenhuma luz 'L' (ok se for escuridão intencional, usando lanterna).")

    # --- saída ---
    print(f"Layout: {data.get('name', '(sem nome)')}  |  tema: {data.get('theme', 'apartments')}  |  "
          f"{g.orig_cols}x{g.orig_rows} células")
    for line in info:
        print("  i ", line)
    for w in warns:
        print("  ! AVISO:", w)
    for e in errors:
        print("  X ERRO:", e)
    if not errors:
        print("OK: sem erros bloqueantes." + (f" ({len(warns)} aviso(s))" if warns else ""))
    return 1 if errors else 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print(__doc__)
        sys.exit(2)
    sys.exit(main(sys.argv[1]))
