# Revisão 3 — tela inteira

Prévia: `09-fim-de-jogo-v3.png`, 1920×1080, cenário real ocupando toda a área, de borda a borda, com interface centralizada. Overlay: `09-fim-de-jogo-v3-ui.png`. Mantida a remoção da frase sobre vidas. Nenhuma alteração na Unity; implementação aguarda aprovação do usuário.

Ferramenta: `image_gen` integrada, seguida de composição com a captura real existente.

## Prompt

```text
Edit this transparent UniDoom game-over interface into a FULL-SCREEN 16:9 menu composition rather than a left-column layout. Output a 1920x1080-proportion wide landscape canvas with genuine alpha transparency. The existing real game screenshot will fill the entire screen behind this interface, edge to edge.
Keep the same established typography, dirty ivory heading, charcoal / oxidized dark-red button treatment, fine worn texture, and Portuguese wording. Reposition the existing elements into one balanced CENTERED full-screen layout: large "FIM DE JOGO" centered horizontally in the upper third around y=24%; short dark red rule centered beneath; smaller "VOCÊ MORREU" centered around y=49%; selected "COMEÇAR DO INÍCIO" action centered around y=66% inside the existing restrained dark strip with small rust-red chevron; "VOLTAR AO MENU" centered around y=77%; tiny subdued "↑ ↓  NAVEGAR     ENTER  CONFIRMAR" centered near y=93%. Keep generous safe margins and legibility across 16:9.
Do not add the deleted sentence about lives. Do not add any other words, numbers, keys, hearts, counters or icons. No photograph, no scenery, no opaque full-canvas panel, no background, no checkerboard, no picture frame, no border, no inset rectangle and no vignette. Empty areas must remain truly transparent so the actual game image can cover ALL of the screen behind the centered menu. This is the same game menu family, only adapted to occupy the whole screen with a centered composition.
```

