# Telas de menus com imagem real do jogo — UniDoom

Status: implementação autorizada. Veja REVISAO.md e as capturas *implementado*.png para a versão final padronizada. Os prompts abaixo documentam a primeira proposta visual.

## Fonte visual

Captura real da câmera do jogo em Play Mode na Unity 6000.5.7f1, URP 17.5.0, cena Assets/_Project/Scenes/EntranceGarden/EntranceGarden.unity, 1920×1080. A arquitetura, os materiais e a iluminação de fundo vêm do projeto, sem cenário inventado por IA.

As interfaces transparentes foram geradas com a ferramenta integrada imagegen e compostas sobre a captura real. O escurecimento é somente uma camada de interface. As telas são propostas visuais; os botões ainda não foram implementados. Continuar e as chaves exibem estados ilustrativos, que deverão refletir o progresso real na implementação.

Fontes de instruções: .codex/SKILL.md (direção visual), .codex/PROMPT_MESTRE.md (preservação visual, compatibilidade e diagnóstico de desempenho) e skill imagegen. O prompt mestre local é de engenharia/otimização, não de criação artística.

A direção de atmosfera considera a referência clássica https://www.moddb.com/mods/total-chaos/images/hotel-lobby indicada pela skill. O fundo das imagens finais é exclusivamente do jogo UniDoom.

## Validação

Os três conceitos usam a mesma captura real selecionada (`referencia-real-jogo.png`), com a câmera girada em 180 graus a partir do spawn para enquadrar o acesso ao campus. A imagem do menu inicial mantém o fundo original; pausa e morte recebem apenas escurecimento uniforme de 25/255 e 48/255, respectivamente. Verificação por amostragem no lado direito do menu inicial: zero diferenças de pixels em relação à captura de origem. Textos e composição dos três PNGs revisados visualmente.

O capturador temporário em `Assets/_TemporaryMenuCapture/Editor/MenuReferenceCapture.cs` foi mantido sem inicialização automática e sem inscrição no update do Editor. A revisão automática rejeitou sua exclusão porque o AGENTS.md exige autorização explícita para exclusões definitivas. O capturador não faz parte da implementação futura dos menus.

Captura obtida em Play Mode. Compilação da ferramenta temporária de captura concluída na Unity. A câmera foi restaurada após a captura e o Play Mode iniciado pela ferramenta foi encerrado. Não foram salvas mudanças em cenas, prefabs ou gameplay. Build, funcionalidades futuras dos menus e desempenho no dispositivo-alvo não foram testados nesta etapa.

## Prompts finais das interfaces transparentes

### Menu inicial — 01-menu-inicial.png

```text
Use case: ui-mockup. Asset type: ONLY the foreground interface for a UniDoom survival-horror game screen, with a truly transparent alpha background, 16:9 landscape canvas. This overlay will be composited onto an unmodified REAL Unity game screenshot. Render only the specified typography, tiny selection marker, hairline rules and selected-action strip. All empty space must be fully transparent, especially the entire right side. NO environment, NO scenery, NO photograph, NO illustration, NO fake transparency checkerboard, NO opaque full-canvas background, NO mockup frame, NO extra text. Original restrained industrial institutional horror visual direction inspired by the classic Total Chaos mod's atmosphere: dirty ivory uppercase condensed sans-serif typography, crisp readable menu items, charcoal selection strip and small oxidized-red marker, tiny amber accents, subtle wear only on large heading. Sharp legible Portuguese text with correct accents. Generous spacing. Render the screen labels exactly as specified. Layout: left menu column begins 10% canvas width. At 18% canvas height a large narrow ivory title "UNIDOOM", height 11% of canvas, with a short rusty-red hairline below. Five vertical menu items beginning 47% canvas height, with 7% canvas height between items: "NOVO JOGO" (selected with small rusty-red marker and dark translucent strip), "CONTINUAR" (dimmed gray), "OPÇÕES", "CRÉDITOS", "SAIR". Main items 2.7% of canvas height. At bottom left tiny "↑ ↓  NAVEGAR     ENTER  CONFIRMAR". Entire right 55% and all spaces between the text are transparent. No slogans or subtitles.
```

### Menu de pause — 02-menu-pause.png

```text
Use case: ui-mockup. Asset type: ONLY the foreground interface for a UniDoom survival-horror game screen, with a truly transparent alpha background, 16:9 landscape canvas. This overlay will be composited onto an unmodified REAL Unity game screenshot. Render only the specified typography, tiny selection marker, hairline rules and selected-action strip. All empty space must be fully transparent, especially the entire right side. NO environment, NO scenery, NO photograph, NO illustration, NO fake transparency checkerboard, NO opaque full-canvas background, NO mockup frame, NO extra text. Original restrained industrial institutional horror visual direction inspired by the classic Total Chaos mod's atmosphere: dirty ivory uppercase condensed sans-serif typography, crisp readable menu items, charcoal selection strip and small oxidized-red marker, tiny amber accents, subtle wear only on large heading. Sharp legible Portuguese text with correct accents. Generous spacing. Render the screen labels exactly as specified. Layout: left menu column begins 10% canvas width. At 23% canvas height tiny gray "UNIDOOM"; at 29% canvas height a large narrow ivory title "PAUSADO", height 9% canvas, short rusty-red hairline below. Four vertical menu items beginning 49% canvas height with 7% canvas height between items: "CONTINUAR" (selected with small rusty-red marker and dark translucent strip, tiny key hint "ESC" at its right), "OPÇÕES", "REINICIAR CHECKPOINT", "MENU INICIAL". Main items 2.7% canvas height. At bottom left tiny "↑ ↓  NAVEGAR     ENTER  CONFIRMAR". Right 55% and empty spaces fully transparent. No cards, sliders or slogans.
```

### Tela de morte — 03-tela-de-morte.png

```text
Use case: ui-mockup. Asset type: ONLY the foreground interface for a UniDoom survival-horror game screen, with a truly transparent alpha background, 16:9 landscape canvas. This overlay will be composited onto an unmodified REAL Unity game screenshot. Render only the specified typography, tiny selection marker, hairline rules and selected-action strip. All empty space must be fully transparent, especially the entire right side. NO environment, NO scenery, NO photograph, NO illustration, NO fake transparency checkerboard, NO opaque full-canvas background, NO mockup frame, NO extra text. Original restrained industrial institutional horror visual direction inspired by the classic Total Chaos mod's atmosphere: dirty ivory uppercase condensed sans-serif typography, crisp readable menu items, charcoal selection strip and small oxidized-red marker, tiny amber accents, subtle wear only on large heading. Sharp legible Portuguese text with correct accents. Generous spacing. Render the screen labels exactly as specified. Layout: centered symmetric composition, center 60% canvas width. Tiny gray "UNIDOOM" at 21% canvas height. Small muted rusty-red "SINAL VITAL PERDIDO" at 29% height. Large narrow ivory slightly weathered title "VOCÊ MORREU" at 36%-46% height, 10% screen height. Short thin dim red flat signal-line motif underneath. Small amber "ÚLTIMO CHECKPOINT" at 56% height. At 61% height modest small key row "CHAVE 01     CHAVE 02     CHAVE 03", with two small amber squares and a third dark gray square. Selected primary action "REINICIAR CHECKPOINT" at 72% height, ivory on a narrow dark rusty-red selection strip with fine border. Secondary gray ivory action "MENU INICIAL" at 81% height. Tiny "ENTER  CONFIRMAR" bottom center. All other pixels fully transparent. No stats, bodies, monsters, cards, slogans or backgrounds.
```

