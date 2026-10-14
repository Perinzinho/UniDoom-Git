"""Utilitários compartilhados pelos scripts de validação e preview.

A lógica de grid aqui (padding de 1 célula + autoShell + regras de "ao ar livre")
espelha exatamente o que o TCMapBuilder.cs faz na Unity. Se mudar uma, mude a outra.
"""
import json
from collections import deque

MARKERS = set("PEILCOX") | set("abcdefghi")   # a..i = chaves 1..9
DOORS = set("D123456789")                      # D = porta comum, 1..9 = trancadas
VALID = set("# ._") | MARKERS | DOORS

KEY_COLORS = {  # mesma convenção do builder
    1: (176, 48, 48), 2: (48, 96, 176), 3: (200, 170, 40), 4: (60, 150, 70),
}
DEFAULT_KEY_COLOR = (130, 70, 160)


def key_color(n):
    return KEY_COLORS.get(n, DEFAULT_KEY_COLOR)


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def walkable(ch):
    return ch not in ("#", " ")


class Grid:
    """Grid com 1 célula de padding. Índices: c[row][col]."""

    def __init__(self, lines, shell=True):
        self.orig_rows = len(lines)
        self.orig_cols = max((len(l) for l in lines), default=0)
        self.rows = self.orig_rows + 2
        self.cols = self.orig_cols + 2
        self.c = [[" "] * self.cols for _ in range(self.rows)]
        for r, line in enumerate(lines):
            for x, ch in enumerate(line):
                self.c[r + 1][x + 1] = ch
        if shell:
            self._shell()

    def at(self, x, r):
        if x < 0 or r < 0 or x >= self.cols or r >= self.rows:
            return " "
        return self.c[r][x]

    def _shell(self):
        add = []
        for r in range(self.rows):
            for x in range(self.cols):
                if self.c[r][x] != " ":
                    continue
                if any(walkable(self.at(x + dx, r + dy))
                       for dx in (-1, 0, 1) for dy in (-1, 0, 1) if dx or dy):
                    add.append((x, r))
        for x, r in add:
            self.c[r][x] = "#"

    def is_outdoor(self, x, r):
        ch = self.at(x, r)
        if ch == "_":
            return True
        if ch in MARKERS:
            n = sum(1 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                    if self.at(x + dx, r + dy) == "_")
            return n >= 2
        return False

    def cells(self):
        for r in range(self.rows):
            for x in range(self.cols):
                yield x, r, self.c[r][x]

    def find(self, chars):
        return [(x, r) for x, r, ch in self.cells() if ch in chars]


def reachable(grid):
    """Simula o jogador: anda, pega chaves (a..i), abre portas 1..9 correspondentes.
    Retorna (conjunto de células alcançadas, conjunto de chaves obtidas)."""
    starts = grid.find("P")
    if not starts:
        return set(), set()
    keys = set()
    seen = set()
    changed = True
    while changed:
        changed = False
        q = deque(starts)
        cur = set(starts)
        while q:
            x, r = q.popleft()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, nr = x + dx, r + dy
                if (nx, nr) in cur:
                    continue
                ch = grid.at(nx, nr)
                if not walkable(ch):
                    continue
                if ch in "123456789" and int(ch) not in keys:
                    continue
                cur.add((nx, nr))
                q.append((nx, nr))
        for x, r in cur:
            ch = grid.at(x, r)
            if ch in "abcdefghi":
                n = ord(ch) - ord("a") + 1
                if n not in keys:
                    keys.add(n)
                    changed = True
        seen = cur
    return seen, keys
