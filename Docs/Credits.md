# Créditos cinematográficos e música do menu

Conteúdo do jogo: `Assets/_Project/Global/Resources/Menus/CreditsContent.txt`. Edite o texto nesse arquivo; aceita títulos Markdown (`#`, `##`, `###`), listas e negrito, formatados pela tela. Os links dos modelos foram removidos a pedido do usuário; nomes dos autores, recursos e licenças continuam no texto. `Docs/Maps/creditos.md` registra o texto editorial fornecido nesta revisão. Os 13 professores estão em ordem alfabética e as instruções editoriais foram substituídas por agradecimentos; nenhum nome de tester foi inventado.

Configuração no Inspector: `Assets/_Project/Global/Resources/Menus/Credits.asset` (`CreditsSettings`). `Content` aponta para o texto. Para adicionar música, atribua um `AudioClip` ao campo `Music` e ajuste `Music Volume`. Sem clipe, os créditos ficam silenciosos. A música é 2D, em loop, segue o volume mestre, toca somente durante os créditos e para ao sair. Usa `ignoreListenerPause` porque o menu pausa o áudio do jogo; nenhum áudio novo foi baixado.

O botão CRÉDITOS abre uma tela inteiramente preta, com texto branco centralizado em uma única coluna. Todo o texto sobe automaticamente desde a parte inferior até sair por completo pelo topo. Não há painel de leitura, barra de rolagem, cabeçalho fixo ou botão na imagem. Esc e o comando de voltar do controle permitem sair antes do fim.

A configuração atual espera 2 s, sobe a 24 unidades de canvas por segundo e aguarda 3 s depois que o último texto sair da tela; então retorna ao menu inicial e para a música. `Scroll Speed`, `Start Delay` e `End Hold` ficam no mesmo `Credits.asset`. Mouse, roda e botões pressionados não interrompem a rolagem. A posição usa tempo absoluto não escalado, portanto continua enquanto o menu pausa o jogo. Implementação em `GameMenus.Credits.cs`.

Espingarda: `WeaponSpriteAnimator` ajusta a proporção da arte opaca à área disponível e refaz o encaixe ao trocar quadro, arma ou resolução. `ShotgunDefinition.asset` habilita `Hud Attach To Screen Edges` e guarda os limites opacos dos 21 quadros. Recortes UV em sprites temporários reutilizados em cache removem a margem transparente do encaixe; não há cópia/edição de pixels nem criação por frame. Uma escala de pixel comum mantém as poses consistentes. A arte encosta no canto inferior direito. Sprites originais, importadores, dano, munição e tempos de animação preservados. Foram conferidos 189 casos de quadros/resoluções, incluindo 4:3, 16:9, ultrawide e orientação vertical.

Nesta revisão, somente a apresentação e o movimento dos créditos foram alterados. Conteúdo, configurações de música, GUIDs e alterações anteriores dos mapas/armas foram preservados. Jogo/Play Mode, reprodução de música, navegação real e build não foram executados. Os testes de gameplay ficam com o usuário. Conforme a última instrução, os prints, vídeo e ferramentas temporárias gerados nesta revisão foram retirados; não são dependências do jogo.

A rolagem cinematográfica foi conferida em modo de edição antes da instrução de deixar os testes com o usuário: progressão por tempo absoluto, início abaixo da tela, saída completa pelo topo, fundo preto, centralização e ausência de controles de leitura. Isso não equivale a testar o fluxo em jogo.


Com a composição cinematográfica atual, a duração estimada em 1920 × 1080 é **4 min 39 s**, incluindo as pausas: conteúdo de 5484 unidades, viewport de 1080 e velocidade de 24 unidades/s. O texto inteiro atravessa a tela. Alterar texto, fonte, resolução ou velocidade altera essa estimativa.


## Corre??es de 09/11/2026

Espingarda: defini??o acrescenta hudBottomCropFraction=0.16 para esconder o corte da arma acima do fundo da m?o observado na refer?ncia fornecida. Encaixe proporcional e bordas preservados; Image tem escala normalizada.

Implementa??o salva; sem abrir Unity, jogo, Play Mode, build, testes ou produzir capturas/previews/v?deos. Confer?ncia em execu??o fica a cargo do usu?rio.
