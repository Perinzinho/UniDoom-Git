# Fim de jogo implementado

Arte aprovada: `09-fim-de-jogo-v3.png`. Captura real da implementação: `10-fim-de-jogo-implementado.png`, em 1920×1080.

Ao chegar a zero vidas, `GameMenus` abre a página própria `GameOver`, com fundo real do campus preenchendo a tela, título e subtítulo da arte aprovada e dois botões reais centralizados. A frase removida não aparece. Mortes com vidas restantes continuam usando o checkpoint existente.

- Começar do início usa o fluxo de nova partida: encerra a sessão esgotada, carrega a primeira cena e cria um jogador com três vidas, saúde cheia, sem chaves e sem checkpoint da partida anterior.
- Voltar ao menu pede a confirmação já existente. Cancelar retorna ao fim de jogo; confirmar encerra a sessão e abre o menu inicial.
- Escape não retoma uma sessão sem vidas. O retorno ao checkpoint continua bloqueado quando as vidas acabam.

O asset `Assets/_Project/Global/Textures/UI/Resources/Menus/GameOverArtwork.png` é uma cópia exata da interface aprovada, com novo GUID e importador próprio. O título, a régua e o subtítulo são recortados dessa arte; os botões permanecem controles Unity com foco, feedback visual e navegação.

## Validação

Compilação concluída na Unity 6000.5.7f1. As 75 verificações programáticas em Play Mode passaram, sem falhas ou interrupções na execução final. Relatório: `validacao-fim-de-jogo.txt`. Inclui dois ciclos de três mortes, retorno ao checkpoint com vidas restantes, bloqueio sem vidas, reinício limpo, navegação por eventos da UI, clique e raycast dos botões, confirmação, cancelamento, retorno ao menu, cursor e preservação de volume, sensibilidade e inversão. A captura foi inspecionada visualmente. Uma execução anterior saiu do Play Mode antes de concluir e foi repetida.

Nenhuma cena ou prefab foi modificado ou salvo. Build do executável, teste em dispositivo e controle físico não foram executados. A ferramenta de validação permanece na área temporária identificada, desativada até invocação explícita pelo menu Tools.
