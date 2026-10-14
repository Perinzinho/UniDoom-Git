# Level design — survival horror estilo Fort Oasis

Leia antes de desenhar o grid. O mapa é uma sequência de **decisões sob pressão**: o jogador
tem pouca munição, pouca luz e informação incompleta. A arquitetura cria o medo; os inimigos só
o confirmam.

## Índice
1. Escala e métricas
2. Estrutura do mapa (hub, loops, chaves)
3. Ritmo e pacing
4. Colocação de inimigos, itens e luzes
5. Padrões prontos de ASCII
6. Como produzir mapas grandes
7. Checklist antes de entregar

---

## 1. Escala e métricas (com `cellSize = 2`, `wallHeight = 3`)

| Elemento | Célula(s) | Metros | Observação |
|---|---|---|---|
| Corredor apertado | 1 de largura | 2 m | o padrão claustrofóbico; use bastante |
| Corredor principal / escada | 2 | 4 m | ainda estreito; permite desviar de um inimigo |
| Quarto / cômodo pequeno | 3×3 a 4×4 | 6–8 m | apartamentos |
| Sala média | 5×4 a 6×5 | 10–12 m | sala de estar, depósito |
| Sala grande (raro) | 8×6 | 16×12 m | refeitório, galpão — 1 por mapa no máximo |
| Porta | 1 célula | 2 × 2.3 m | sempre em linha de parede |
| Pé-direito | — | 3 m | baixo = opressivo; túneis podem ter 2.6 m |
| Praça/rua (`_`) | largura 4–6 | 8–12 m | exteriores ficam estreitos por causa do fog |

Mapas completos: 25–45 células de lado (50–90 m) são suficientes para 15–40 min de jogo
quando bem interligados. Comece menor do que parece necessário.

Se o usuário pedir mais "espaço" ou mais "aperto", mude `cellSize` (1.5–2.5), não o desenho.

## 2. Estrutura do mapa

**Hub e raios.** Um espaço central reconhecível (corredor principal, pátio, praça) conecta
as áreas; o jogador sempre volta a ele, o que cria familiaridade — e familiaridade é o que o
horror vai quebrar.

**Loops e atalhos (estilo Dark Souls / Resident Evil).** Todo mapa com ≥ 4 regiões precisa de
pelo menos 1 loop: uma porta que, aberta pelo lado de dentro, reconecta uma área já visitada.
O validador reporta "Loops independentes"; mire em 2–4 num mapa médio. Atalhos podem ser portas
que só abrem de um lado (trate no gameplay) ou passagens que o jogador vê antes de alcançar.

**Chave → porta (progressão).** Convenção do grid: porta trancada `1`..`9`, chave `a`..`i`
(a abre 1, b abre 2...). Regras:
- A chave fica **longe** da porta, atrás de um risco (sala com inimigo, área escura, corredor
  apertado), e o caminho de volta passa por algo que mudou (inimigo novo, luz apagada).
- O jogador deve **ver a porta trancada antes** de achar a chave.
- Nunca ponha a chave atrás da própria porta (o validador acusa soft-lock).
- Encadeie no máximo 2–3 chaves por mapa; mais que isso vira backtracking cansativo.

**Becos sem saída recompensam.** Beco = região com uma só porta: ideal para item raro, nota
da história ou emboscada. Mas não termine o caminho principal em beco.

**Linha de visão.** Corredor reto longo termina em algo (porta, luz piscando, vulto). Curvas e
portas escondem; salas em L e com pilar impedem ver a sala inteira da entrada. Evite saguões
abertos onde se vê tudo — o fog compensa, mas o desenho deve sustentar.

## 3. Ritmo e pacing

Alterne ciclos de tensão → alívio → escalada:

1. **Respiro / sala segura:** sala pequena, bem iluminada (`L` ×2), porta única, 1–2 itens,
   nenhum `E` em 6 células. Aparece a cada 3–4 regiões e logo antes de picos.
2. **Aproximação:** corredor escuro, som, uma luz que pisca. Zero inimigos.
3. **Encontro:** 1–2 inimigos (`E`) numa sala com rota de fuga (2 portas) — nunca beco para o jogador.
4. **Pico (1 por mapa):** sala grande ou praça com 3–4 `E`, recursos suficientes por perto
   (`I` ×2) mas visíveis só depois de entrar, e uma saída óbvia.
5. **Alívio curto**, depois de novo.

Falsos alarmes (sala vazia onde o jogador esperava ataque) são tão importantes quanto ataques.
Regra prática: ~⅓ das regiões vazias, ~⅓ com 1 inimigo, ~⅓ com 2+ ou com armadilha de ambiente.

## 4. Inimigos, itens e luzes

- **Escassez:** itens (`I`) < inimigos (`E`); mire em ~0.5–0.8 itens por inimigo. O jogador
  precisa decidir entre lutar e fugir. (O validador avisa se itens > inimigos.)
- **Nunca** coloque `E` a ≤ 3 células do `P` (spawn-kill).
- Itens em lugares que exigem risco: depois de uma porta, no fim de corredor escuro, perto de um
  `E`. Evite itens "de graça" no meio de salas vazias.
- Inimigos perto de portas ficam de costas para quem entra ou dormem no canto; evite colocar
  em linha reta com a entrada (tiro fácil).
- Luzes: veja `art-direction.md §4`. Deixe ≥ 1 região grande sem luz (lanterna-only) por mapa.
- Exteriores (`_`): poste de luz `L` a cada 6–8 células, entre eles trechos de sombra.
- Props `C`/`O` servem de cobertura e para estreitar caminhos; nunca bloqueiam portas.

## 5. Padrões prontos de ASCII

Legenda: `.` piso interno, `_` ao ar livre, `D` porta, `1` trancada, `a` chave, `P` início,
`E` inimigo, `I` item, `L` luz, `C` caixa, `O` barril, `X` saída. Paredes `#` são opcionais: o
`autoShell` cria paredes ao redor de qualquer área andável.

**Prédio de apartamentos (corredor 1 célula com cômodos dos dois lados):**
```
...... ...... ......
..L... ..C.E. ...I..
...... ...... ......
   D      D      D
.........L..........
   D      D      D
...... ...... ......
.I.L.. ....E. ..C...
...... ...... ......
```

**Túneis com loop (anel de 1 célula em volta de um bloco maciço; portas dividem o anel):**
```
.L.......D.....L.
.               .
D               D
.               .
.L....E..D.....L.
```

**Praça com ruas (exterior, `_`):**
```
____________
__L______L__
__________E_
_____O______
__E_____C___
__L______L__
____________
```

Dica: salas separadas por **1 célula de vazio** (` `) + porta `D` nessa célula é o jeito mais
limpo de fazer uma parede com porta.

## 6. Como produzir mapas grandes

Para mapas maiores que ~30×20, **não digite o grid à mão**: gere por código (Python) com
funções `rect(c0, r0, c1, r1, ch)` para "esculpir" salas e corredores, `door(c, r, ch)` para
abrir portas em linhas de parede e `put(c, r, ch)` para marcadores, com `assert` de que a célula
alvo é piso. Normalize o comprimento das linhas (`ljust`) antes de salvar o JSON. Isso evita as
linhas desalinhadas que são a causa nº 1 de mapas quebrados.

Fluxo: desenhe a **topologia** primeiro (regiões + portas + chave/porta), valide, **só depois**
distribua `E`, `I`, `L`, `C`, `O`.

## 7. Checklist antes de entregar

- [ ] `validate_layout.py` sem erros; avisos lidos e justificados ou corrigidos.
- [ ] Exatamente 1 `P`; ≥ 1 `X` alcançável.
- [ ] Todas as portas trancadas têm chave alcançável antes delas.
- [ ] ≥ 1 loop (se ≥ 4 regiões) e ≥ 1 sala segura.
- [ ] Itens < inimigos; nenhum inimigo colado no início.
- [ ] Preview PNG revisado: caminho principal legível, gargalos e becos intencionais.
- [ ] Tema escolhido combina com o tipo de ambiente (apartments/tunnels/streets/facility).
