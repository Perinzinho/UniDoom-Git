---
name: total-chaos-unity-maps
description: Cria mapas/fases para a Unity no estilo visual e de level design do Total Chaos (total conversion de survival horror para Doom II/GZDoom publicado no ModDB por Sam Prebble, ilha Fort Oasis). Desenha o layout como grid ASCII em JSON, valida (conectividade, chaves/portas, pacing), gera preview PNG e entrega scripts C# de Editor que constroem o mapa 3D com paredes, portas, luzes que piscam, fog denso, grain, vinheta e paleta suja e dessaturada. Use SEMPRE que o usuário pedir mapa, fase, nível, cenário, level design, greybox, blockout, ambientação ou atmosfera de horror na Unity, mencionar Total Chaos, Fort Oasis, estilo Doom/GZDoom, survival horror retrô, "map do GZDoom" ou querer gerar/estender mapas proceduralmente na Unity, mesmo que não cite esta skill. Also triggers on English requests for Unity level generation, horror map blockout, Doom-style or Silent Hill-like environments.
---

# Total Chaos Maps (Unity)

Esta skill faz a IA projetar e gerar **mapas de survival horror na Unity** com a linguagem
visual e o ritmo de level design do Total Chaos (versão GZDoom/ModDB): concreto encardido, paleta
dessaturada, névoa pesada, luz escassa e motivada, granulado analógico, corredores apertados,
loops, portas com chave, escassez de recursos.

O Claude não roda a Unity aqui. O que ele entrega é: (1) um **layout em JSON** (grid ASCII)
validado e visualizado, e (2) **scripts C# de Editor** que, dentro da Unity, transformam esse
JSON em cena jogável com atmosfera aplicada. Como o layout é texto, dá para iterar rápido
("aumenta a sala da chave", "mais um loop") sem abrir a Unity a cada mudança.

## Arquivos da skill

| Arquivo | Quando ler/usar |
|---|---|
| `references/art-direction.md` | **Sempre** antes de decidir tema, cores, luz, fog, pós. É a "bíblia de estilo". |
| `references/level-design.md` | Ao desenhar ou revisar o layout (escala, loops, chaves, pacing, padrões ASCII). |
| `references/unity-implementation.md` | Ao entregar scripts, instalar, afinar, depurar erro ou estender (andares, prefabs, NavMesh). |
| `scripts/validate_layout.py` | Depois de escrever o JSON. Detecta soft-locks, portas soltas, métricas. |
| `scripts/preview_layout.py` | Gera PNG top-down do layout para o usuário aprovar. |
| `assets/examples/apartamentos_01.json` | Exemplo completo e válido (7 inimigos, 1 chave, loops, pátio). |
| `assets/unity/Editor/*.cs`, `assets/unity/Runtime/*.cs` | Scripts C# a entregar ao usuário (copiar sem alterações, salvo ajuste pedido). |

## Fluxo de trabalho

### 1. Entender o pedido (sem interrogatório)

Descubra só o que falta, no máximo 1–2 perguntas, e assuma o resto declarando a suposição:

- Tipo de ambiente / "capítulo": apartamentos, túneis, rua/praça, mina/instalação? (define o tema)
- Tamanho e duração aproximados (default: mapa médio ~30×20 células, 6–8 regiões).
- Versão da Unity e pipeline. **Default: Unity 6 + URP.** Se for Built-in, avise do ajuste de `#define TC_URP`.
- Objetivo do mapa (achar chave → saída? sobreviver até o rádio?). Default: chave → porta → saída.

Se o usuário só diz "faz um mapa", gere o apartamento/corredor padrão e pergunte depois o que ajustar.

### 2. Ler as referências

Leia `references/art-direction.md` e, para o layout, `references/level-design.md`. Use as
métricas e o checklist de lá; não improvise escala.

### 3. Projetar o layout (grid ASCII em JSON)

Desenhe a **topologia primeiro** (regiões, portas, chave→porta, loops), depois marcadores.
Para mapas maiores que ~30×20, gere o grid por código Python (esculpir `rect`, abrir `door`,
`put` marcadores com `assert`) e normalize larguras — digitar à mão causa linhas desalinhadas.

**Legenda do grid** (uma célula = `cellSize` metros, padrão 2 m):

| Char | Significado |
|---|---|
| ` ` (espaço) | vazio. Ao lado de área andável vira parede automaticamente (`autoShell`) |
| `#` | parede explícita (opcional) |
| `.` | piso interno (com teto) |
| `_` | ao ar livre (sem teto: ruas, pátios) |
| `D` | porta comum, **sempre numa linha de parede** (paredes dos 2 lados opostos) |
| `1`–`9` | porta trancada que exige a chave de mesmo número |
| `a`–`i` | chave 1–9 (a abre `1`, b abre `2`, …) |
| `P` | início do jogador (exatamente 1) |
| `E` | ponto de spawn de inimigo |
| `I` | ponto de item/loot/nota |
| `X` | saída / objetivo |
| `L` | luz (lâmpada no teto; poste se ao ar livre) |
| `C` / `O` | caixa / barril (cobertura e clima; nunca ao lado de porta) |

Marcadores (`P E I L C O X a-i`) valem como piso. Marcador cercado por `_` (≥2 vizinhos) é
tratado como ao ar livre.

**Esquema do JSON** (todos os campos exceto `grid` são opcionais):

```json
{
  "name": "TC_Apartamentos_01",
  "theme": "apartments",
  "cellSize": 2.0,
  "wallHeight": 3.0,
  "doorHeight": 2.3,
  "ceiling": true,
  "autoShell": true,
  "textureMeters": 3.0,
  "seed": 1337,
  "grid": ["P....  ...", "..L.D......"]
}
```

`theme`: `apartments` | `tunnels` | `streets` | `facility`. Veja `assets/examples/apartamentos_01.json`.

### 4. Validar e visualizar

Rode, no diretório `scripts/` (os dois importam `tc_layout.py`):

```bash
python validate_layout.py caminho/layout.json
python preview_layout.py caminho/layout.json caminho/preview.png
```

- **Erros** (exit 1): soft-lock (chave atrás da própria porta), saída/item inalcançável,
  porta trancada sem chave, `P` ausente, caractere inválido. Corrija e rode de novo.
- **Avisos**: porta fora de linha de parede, mapa linear sem loop, itens > inimigos, inimigo
  colado no início, pouca luz. Corrija ou justifique ao usuário.
- Se o ambiente permitir arquivos, **mostre o PNG** ao usuário e peça aprovação do traçado
  antes de falar de Unity. Mudar o ASCII é barato; mudar cena pronta, não.

### 5. Entregar

Entregue, nesta ordem, usando os arquivos de saída da conversa (e `present_files` quando existir):

1. O `layout.json` final e o `preview.png`.
2. Os scripts C# de `assets/unity/` (os 7 arquivos) — de preferência já copiados juntos numa pasta
   `TotalChaos/` com `Editor/` e `Runtime/` (ou um `.zip`), para o usuário só arrastar para `Assets/`.
3. Um resumo curto: tema escolhido e por quê, contagem (regiões, portas, loops, inimigos,
   itens), o caminho crítico (início → chave → porta → saída), e **os 5 passos de instalação**
   de `references/unity-implementation.md §2`.
4. Dois ou três ajustes que o usuário provavelmente vai querer ("mais escuro", "mais apertado",
   "mais inimigos") e como fazer (qual campo/valor mudar).

Seja direto sobre os limites: os scripts não foram compilados numa Unity real por quem os
escreveu; se der erro, o usuário deve colar a mensagem completa para correção.

### 6. Iterar

Pedidos de mudança viram edições no **grid** (ou nos números do `TCTheme.cs`), seguidas de nova
validação e novo preview. Para extensões (vários andares, escadas, prefabs próprios, NavMesh,
decals, lanterna, interação de porta) siga `references/unity-implementation.md §7 e §9` e
altere o builder, mantendo `scripts/tc_layout.py` e `TCMapBuilder.cs` coerentes (a lógica de
grid existe nos dois e precisa ficar idêntica).

## Princípios (o porquê por trás das regras)

- **Estilo ≠ assets.** O Total Chaos é obra de terceiros. Reproduza a linguagem (paleta, fog,
  escassez, arquitetura, ritmo), nunca texturas, modelos, sons, nomes de mapa ou layouts do mod.
  As texturas aqui são procedurais; sugira ao usuário assets próprios ou CC0 se quiser mais
  detalhe.
- **Valor antes de matiz.** O clima vem do contraste entre poças de luz e escuridão, não da
  cor. Quando em dúvida, escureça o ambiente e acenda uma luz motivada.
- **Escassez cria medo.** Menos itens que inimigos, itens atrás de risco, salas seguras raras.
- **Topologia antes de decoração.** Loops, atalhos e chave→porta decidem se o mapa é bom; props
  e luzes só afinam.
- **Validar barato, construir caro.** O layout é texto; só vá à Unity com layout aprovado.
- **Um estilo por mapa.** Um tema por JSON; para misturar interior e exterior use `_` com tema
  `streets`, ou separe em mapas/cenas.

## Armadilhas frequentes

- Linhas do `grid` com comprimentos diferentes (o builder completa com vazio, mas o desenho
  provavelmente está torto): normalize.
- `D` em lugar que não é linha de parede (porta flutuando no meio da sala).
- Esquecer o espaço de 1 célula entre duas salas: sem ele não há parede/porta entre elas.
- Chave `a` sem porta `1` (ou o contrário) — o validador avisa.
- Entregar C# sem dizer onde colocar (`Editor/` é obrigatória) ou sem o `Runtime/`.
- Pedir ao usuário "abra a Unity e veja" sem antes ter validado/preview do layout.
