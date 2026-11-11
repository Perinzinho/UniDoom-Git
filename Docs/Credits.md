# Créditos cinematográficos e música do menu

Conteúdo do jogo: `Assets/_Project/Global/Resources/Menus/CreditsContent.txt`. Edite o texto nesse arquivo; aceita títulos Markdown (`#`, `##`, `###`), listas e negrito, formatados pela tela. Os links dos modelos foram removidos a pedido do usuário; nomes dos autores, recursos e licenças continuam no texto. `Docs/Maps/creditos.md` registra o texto editorial fornecido nesta revisão. Os 13 professores estão em ordem alfabética e as instruções editoriais foram substituídas por agradecimentos; nenhum nome de tester foi inventado.

Configuração persistente: `Assets/_Project/Global/Resources/Menus/Credits.asset` (`CreditsSettings`). `Content` aponta para o texto; a música salva é **HiddenKing** e a velocidade salva é **42**. `CreditsSettings` reaplica 42 ao carregar/validar o asset. Se a referência de música estiver vazia ou apontar para outra faixa, carrega `Resources/Audio/Credits/HiddenKing`; o menu também usa HiddenKing/42 se a configuração inteira estiver indisponível. `Music Volume`, `Start Delay` e `End Hold` continuam configuráveis. A música é 2D, em loop, segue o volume mestre, toca somente durante os créditos e para ao sair. Usa `ignoreListenerPause` porque o menu pausa o áudio do jogo.

O mesmo áudio foi movido, sem cópia/conversão, de `Assets/_Project/Global/Audio/credits/HiddenKing.wav` para `Assets/_Project/Global/Resources/Audio/Credits/HiddenKing.wav`. Bytes, importador e GUID `db143bb81fcd49f4f9e065cec2dc0ab0` preservados; referências por GUID continuam válidas. O carregamento de fallback só ocorre ao iniciar os créditos, não durante a construção do menu. Os outros arquivos de música permanecem nos locais anteriores.

O botão CRÉDITOS abre uma tela inteiramente preta, com texto branco centralizado em uma única coluna. Todo o texto sobe automaticamente desde a parte inferior até sair por completo pelo topo. Não há painel de leitura, barra de rolagem, cabeçalho fixo ou botão na imagem. Esc e o comando de voltar do controle permitem sair antes do fim.

A configuração atual espera 2 s, sobe a **42 unidades de canvas por segundo** e aguarda 3 s depois que o último texto sair da tela; então retorna ao menu inicial e para a música. `Scroll Speed`, `Start Delay` e `End Hold` ficam no mesmo `Credits.asset`; a velocidade é fixada em 42 também nos defaults/fallbacks do código. Mouse, roda e botões pressionados não interrompem a rolagem. A posição usa tempo absoluto não escalado, portanto continua enquanto o menu pausa o jogo. Implementação em `GameMenus.Credits.cs`.

Espingarda: `WeaponSpriteAnimator` ajusta a proporção da arte opaca à área disponível e refaz o encaixe ao trocar quadro, arma ou resolução. `ShotgunDefinition.asset` habilita `Hud Attach To Screen Edges` e guarda os limites opacos dos 21 quadros. Recortes UV em sprites temporários reutilizados em cache removem a margem transparente do encaixe; não há cópia/edição de pixels nem criação por frame. Uma escala de pixel comum mantém as poses consistentes. A arte encosta no canto inferior direito. Sprites originais, importadores, dano, munição e tempos de animação preservados. Foram conferidos 189 casos de quadros/resoluções, incluindo 4:3, 16:9, ultrawide e orientação vertical.

Nesta revisão, somente a apresentação e o movimento dos créditos foram alterados. Conteúdo, configurações de música, GUIDs e alterações anteriores dos mapas/armas foram preservados. Jogo/Play Mode, reprodução de música, navegação real e build não foram executados. Os testes de gameplay ficam com o usuário. Conforme a última instrução, os prints, vídeo e ferramentas temporárias gerados nesta revisão foram retirados; não são dependências do jogo.

A rolagem cinematográfica foi conferida em modo de edição antes da instrução de deixar os testes com o usuário: progressão por tempo absoluto, início abaixo da tela, saída completa pelo topo, fundo preto, centralização e ausência de controles de leitura. Isso não equivale a testar o fluxo em jogo.


A estimativa histórica de **4 min 39 s** usava conteúdo de 5484 unidades, viewport de 1080 e velocidade de 24 unidades/s. Com essas mesmas alturas e velocidade 42, seriam aproximadamente **2 min 41 s**, incluindo as pausas. A duração atual depende da altura real do conteúdo e da resolução; não foi medida em jogo nesta alteração.

Validação de 10/11/2026: configuração serializada, referência/GUID do áudio e carregamento por Resources conferidos; bytes do WAV preservados. Compilação C# e verificação isolada de restauração de valores antigos, fallback de música e cálculo de rolagem. Unity, Play Mode, reprodução de áudio, build e capturas não executados; testes finais com o usuário.

## HUD da espingarda — 09/11/2026

A definição da espingarda usa `hudBottomCropFraction = 0.16` para ocultar abaixo da tela o corte inferior da arma visto na imagem fornecida. Proporção, encaixe por resolução e sprites originais preservados; escala da Image normalizada. Implementação salva, sem executar jogo ou gerar capturas.

## Contador de FPS — 09/11/2026

A tela `OPÇÕES`, acessível pelo menu inicial e pelo menu de pausa, possui a opção `Mostrar FPS`. Ela fica desativada por padrão e é salva em `PlayerPrefs` pela chave `UniDoom_ShowFPS`. Quando ativada, exibe a média de quadros por segundo no canto superior direito somente durante o gameplay; menus, documentos, interações e carregamentos ocultam o indicador. A leitura usa tempo não escalado e é atualizada a cada meio segundo.
