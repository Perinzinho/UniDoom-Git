# Revisão solicitada e implementação

Implementação concluída. Capturas finais reais da Unity em 1920×1080:

- 01-menu-inicial-implementado.png
- 02-menu-pause-implementado.png
- 03-tela-de-morte-implementada.png
- 04-opcoes-implementadas.png
- 05-confirmacao-voltar-menu.png

Compilação concluída na Unity 6000.5.7f1. As 35 verificações em Play Mode passaram; relatório em validacao-menus.txt. Foram validados novo jogo, bloqueio de inputs, navegação de botões, pausa, opções, dimensões dos sliders, reinício vivo e após morte, preservação de chaves no retry, cancelamento/aceitação do aviso e encerramento de sessão. As capturas também foram inspecionadas visualmente. Os valores de 1.37x/37% na captura das opções são temporários da checagem. Volume em 100% e sensibilidade em 1x autorizados pelo usuário após a validação; confirmação em preferencias-finais.txt. A inversão do eixo Y foi preservada nessa redefinição.

Build do executável, teste de tela cheia no executável, saída do executável e profiling de desempenho não foram executados. A ferramenta de validação permaneceu na área temporária identificada, sem solicitação de execução pendente; nenhuma cena ou prefab foi modificado/salvo pela validação.

Menu inicial: Novo jogo, Opções, Créditos e Sair. Sem Continuar. Não há salvamento permanente de progresso; a partida atual preserva os checkpoints e as chaves enquanto está em andamento.

Morte: VOCÊ MORREU, TENTAR NOVAMENTE e VOLTAR AO MENU. Sem lista de chaves, sem painel de sinais vitais e sem o texto amarelo de ponto de retorno. Pausa e morte implementadas usam o cenário real congelado no momento em que são abertas.

As três telas usam o atlas compartilhado MenuHeadings, com o mesmo material, escala das letras, alinhamento, régua, botões, cores, espaçamento e rodapé. Menu inicial e pausa têm ações nas mesmas posições. A morte usa a mesma coluna de ações.

Atlas gerado com a ferramenta integrada imagegen e salvo também em overlays/titulos-padronizados.png. Prompt do atlas:

```text
Use case: ui-mockup. Asset type: a transparent typography atlas for three matching UniDoom horror game menu headings. This is NOT a complete menu and NOT a poster. Output ONE image with genuine alpha transparency and no background. Wide rectangular canvas, three equal horizontal rows, all text on one line per row. Exactly three text elements: first row "UNIDOOM", second row "PAUSADO", third row "VOCÊ MORREU". Portuguese accents correct, no other text whatsoever. Each row left aligned to the same vertical grid, at x=6% of image width. Heading baseline centered inside its own third of the image; keep generous transparent separation between rows for clean sprite cropping. All three headings use EXACTLY the same font, cap height, weight, texture treatment and flat dirty ivory color. Fixed cap height about 16% of total canvas height for all three rows; use a very tall narrow heavy condensed sans-serif with consistent letter spacing so VOCÊ MORREU fits on one line. Quiet fine weathering and tiny material imperfections like old ivory painted institutional signage, restrained classic survival horror. Flat typography, sharp readable edges, very subtle dark drop shadow only. No bevels, no 3D extrusion, no marble texture, no glow, no icons, no lines, no selection bars, no buttons, no keys, no yellow text, no captions, no logos other than the specified UNIDOOM lettering, no scenery, no photograph, no checkerboard. Empty pixels are transparent. The three rows must look like a professionally standardized single UI family.
```

Voltar ao menu, tanto na pausa quanto na morte, exige a confirmação: Você tem certeza? Os dados não serão salvos. Cancelar preserva a tela e a partida. Confirmar encerra a sessão; Novo jogo inicia uma sessão limpa.

Opções: sensibilidade, inversão do eixo Y, volume geral e tela cheia. Preferências são persistidas; progresso da partida não é persistido.

## Prompt da nova arte

```text
Use case: ui-mockup. Create a NEW redesigned death-screen foreground interface for UniDoom, an oppressive realistic university-campus survival horror game. Output a 16:9 wide image with TRUE TRANSPARENT ALPHA BACKGROUND. This is an interface-only overlay to composite on an unchanged REAL Unity game screenshot. NO scenery, NO photograph, NO environment, NO full-canvas opaque background or fake checkerboard. Render only the following original interface design.
Visual direction: restrained and grounded classic industrial survival horror; weathered dirty ivory title lettering, charcoal, oxidized dark red, tiny dull amber accent, sharp readable Portuguese condensed sans serif type, extremely sparse fine rules, subtle texture on title only. Avoid neon, gore, skulls, cartoon, glossy buttons, excessive bevels and glows, medical HUD, ECG motifs, keys inventory rows, boxes or cards around the layout.
Composition: move the entire death interface to the LEFT SIDE, leaving the entire right 55% fully transparent. At x=10%, y=15% tiny tracked gray-ivory "UNIDOOM". At x=10%, y=23% a thin short dark red line. At x=10%, y=28%-46% very large narrow dirty ivory heading across TWO LINES, exact text "VOCÊ" on the first line, "SUCUMBIU" on the second. The heading is subtly worn, imposing and beautifully typeset. At x=10%, y=55% small dull amber line "PONTO DE RETORNO / INÍCIO". At x=10%, y=61% small gray ivory explanation "Recomeçar no último ponto de retorno.". At x=10%, y=72% selected primary action "TENTAR NOVAMENTE" in clear uppercase ivory on a very restrained charcoal-dark red selection strip, tiny red marker at left. At x=10%, y=81% secondary action "VOLTAR AO MENU" in slightly dimmer ivory. At x=10%, y=93% a small subdued navigation legend "↑ ↓  NAVEGAR     ENTER  CONFIRMAR". All text aligned to one vertical grid, comfortably separated. No extra copy, no slogans, no numbers beyond the specified text. Portuguese accents must be correct. All empty space transparent, right side totally transparent.
```

## Correção final do título solicitada pelo usuário

```text
Edit ONLY the large heading of this transparent UniDoom death-screen interface. Replace the large two-line text "VOCÊ SUCUMBIU" with exactly "VOCÊ" on the first line and "MORREU" on the second line. Match the existing dirty-ivory weathered condensed lettering, left alignment, line heights and hierarchy. Keep the small UNIDOOM brand, red rule, point-of-return caption, explanatory line, TENTAR NOVAMENTE button, VOLTAR AO MENU action, navigation footer, layout and colors unchanged. Preserve genuine alpha transparency and the entirely transparent empty right side. Do not add a background, environment, card, checkerboard, watermark or new text.
```

