# Puzzles físicos sob pressão — Biblioteca da Unimar

> Pedido posterior: para as cinco chaves da biblioteca, o usuário prefere enigmas simples de leitura e dedução. A proposta atual está em [Puzzles de leitura](CampusLibrary-Puzzles-Leitura.md). Este documento permanece como histórico de alternativas físicas; nenhuma foi implementada.

**Ideias para escolha e protótipo; nenhuma implementada.** Linha escolhida pelo usuário: ferramentas, manipulação do cenário e perigo, com referências em Amnesia e Resident Evil. As propostas anteriores foram removidas.

Mapa: `CampusLibrary`, a biblioteca da Unimar. [Contexto](../../Docs/Maps/CampusLibrary.md) · [Compatibilidade com o jogo](../../Docs/Maps/CampusLibrary-Mecanicas.md).

## Referências pesquisadas

- **Amnesia: The Bunker:** obstáculos podem ser abordados por ferramentas, força, disparos ou pela própria ameaça. Também há portas impedidas fisicamente por objetos, mesmo quando a fechadura não é o problema. [Guia dos obstáculos e métodos](https://primagames.com/tips/how-to-break-open-wooden-doors-and-padlocks-in-amnesia-the-bunker), por Nikola L; [apresentação da Frictional Games](https://store.steampowered.com/app/1944430/Amnesia_The_Bunker/).
- **Resident Evil 7 — Bedroom:** manipular o quarto, acompanhar a aproximação de Marguerite e restaurar o ambiente cria tensão entre investigar e ocultar a ação. [Walkthrough](https://www.gamesradar.com/how-to-escape-the-bedroom-in-resident-evil-7s-new-dlc/), por Leon Hurley. A adaptação abaixo aproveita a ideia de alterar e recompor um objeto; não presume que um zumbi de UniDoom já saiba inspecionar uma sala.
- **Penumbra: Overture:** gavetas, alavancas e objetos manipuláveis em 3D participam da resolução do espaço. [Descrição oficial da Frictional Games](https://store.steampowered.com/app/22180/Penumbra_Overture/).

Pesquisa documental em 07/11/2026, sem execução desses jogos. As situações a seguir são adaptações autorais para a biblioteca.

## 1. A gaveta não está trancada

**Local:** arquivo de madeira atrás do atendimento. **Ferramenta:** chave de fenda. **Referência:** obstruções físicas de The Bunker e gavetas de Penumbra.

A gaveta com a recompensa abre poucos centímetros e bate em alguma coisa. Pelo vão, é possível ver uma peça metálica atravessada atrás dela. A fechadura já está aberta: procurar outra chave não resolve.

**Como resolver:** retirar a pequena tampa de manutenção lateral → observar que a peça está presa pela gaveta superior → usar a chave de fenda para soltar o limitador dessa gaveta → puxá-la até expor a peça → retirar a obstrução → abrir a gaveta inferior. Cada etapa muda algo visível e permite testar o movimento de novo.

**Onde fica difícil:** trabalhar na lateral exige ficar de costas para um acesso do hall. Um inimigo pode obrigar o jogador a interromper a desmontagem, circular pelo balcão e voltar pelo outro lado. Forçar a gaveta com golpes é uma alternativa ruidosa e mais cara, a balancear.

**Erro recuperável:** usar a ferramenta no ponto errado apenas não libera a peça; forçar causa barulho, sem destruir a recompensa. A desmontagem concluída permanece concluída.

**Protótipo:** um móvel, duas gavetas, uma tampa e uma peça presa. Manipulação guiada por pontos de interação, sem exigir física livre de todo o cenário.

## 2. O objeto ficou dentro do balcão de devoluções

**Local:** caixa de devoluções e carrinho de livros. **Ferramentas:** gancho curto e chave de fenda. **Referência:** uso espacial de ferramentas em Penumbra.

Uma pequena caixa com a recompensa caiu no caminho interno da devolução e ficou apoiada atrás de uma chapa. O jogador consegue vê-la pela abertura, mas puxar diretamente a prende ainda mais.

**Como resolver:** abrir a inspeção lateral → posicionar a bandeja de coleta sob a saída → usar o gancho para levantar a borda da caixa → afastar a chapa de retenção com a chave de fenda → deixar a caixa deslizar até a bandeja. O jogador deduz a sequência observando apoios, folgas e direção de queda.

**Onde fica difícil:** é preciso alternar entre a frente e a lateral do móvel. O inimigo ocupa a rota mais curta; interromper uma ação para combater faz o jogador perder a oportunidade de executar a próxima, mas não todo o trabalho.

**Erro recuperável:** sem a bandeja, a caixa cai num ponto visível no piso, de recuperação mais exposta. Um enganche errado devolve a peça ao apoio anterior. Não há sequência secreta nem perda definitiva.

**Protótipo:** caixa, chapa articulada, bandeja em dois encaixes e gancho com alcance limitado. O caminho de queda pode ser guiado; não precisa simular centenas de livros.

## 3. Desmontar é fácil; sair sem deixar o móvel batendo é o problema

**Local:** nicho de estudo com um armário de manutenção. **Ferramentas:** chave de fenda e um calço de borracha. **Referência:** modificar e recompor o espaço sob ameaça, de Bedroom.

A recompensa está atrás da tampa de um mecanismo antigo. Ao soltar a tampa, um braço metálico começa a bater repetidamente no gabinete. O jogador pode alcançar o objetivo, mas a batida deixa aquele ponto continuamente exposto à investigação dos inimigos.

**Como resolver com preparação:** examinar o mecanismo → encaixar o calço entre o braço e o gabinete → retirar a tampa → alcançar a recompensa → recolocar a tampa e só então recuperar o calço. Também é possível agir com pressa, pegar o objetivo e sair por uma rota já preparada, aceitando a perseguição local.

**Onde fica difícil:** o espaço de trabalho é um nicho com uma única entrada. O jogador precisa reconhecer a função do calço antes de provocar o ruído ou administrar o combate enquanto tenta recompor a tampa.

**Erro recuperável:** a tampa permanece próxima e pode ser recolocada. O inimigo investiga um ruído localizado; não conhece automaticamente a posição atual do jogador. Ruídos de equipamentos precisam de integração própria — o jogo já reage a tiros, não a qualquer AudioSource.

**Protótipo:** tampa, braço oscilante, um ponto de calço e um inimigo. O ruído e a ameaça permanecem locais, sem repetir a sirene e a horda de Direito.

## 4. A ameaça pode fazer a parte pesada

**Local:** passagem de serviço bloqueada por uma estrutura de madeira danificada. **Ferramenta opcional:** alavanca. **Referência:** diferentes maneiras de romper obstáculos em The Bunker.

O bloqueio tem uma escora rachada, visível do lado acessível. O compartimento de interesse fica além dele. O jogador pode gastar tempo usando uma alavanca, gastar munição se o material permitir ou atrair um inimigo de ataque pesado para atingir a escora.

**Como resolver usando a ameaça:** identificar a parte estrutural fraca → liberar uma saída lateral para o próprio jogador → posicionar-se diante da escora → esperar o ataque anunciado → sair da trajetória para que o golpe alcance a estrutura. O inimigo continua perigoso depois da abertura.

**Onde fica difícil:** provocar o ataque exige proximidade, e a saída lateral precisa estar livre. Um segundo inimigo pode ocupar a travessa, mudando o momento de tentar. Atirar pode atrair outras ameaças elegíveis para a origem do disparo.

**Erro recuperável:** recuar cedo faz o golpe errar; tarde demais causa dano normal de combate. A escora conserva o dano recebido. Há uma solução por ferramenta caso o inimigo seja morto antes de romper a estrutura.

**Protótipo:** uma escora destrutível, uma rota de recuo e um atacante compatível. Dano de inimigos ao cenário e o comportamento necessário ainda precisam ser criados/verificados; não são capacidades confirmadas dos zumbis atuais.

## 5. O equipamento passa; a alça, não

**Local:** sala coletiva, porta e bancada de serviço. **Ferramentas:** chave de boca e grampo de transporte. **Referência:** manipulação e encaixe espacial de Penumbra.

Um equipamento de conservação em carrinho contém a recompensa numa bandeja travada. A bancada de serviço permite abrir o equipamento, mas ele não atravessa a porta na posição em que foi encontrado. A largura extra vem de uma alça lateral presa pelo apoio de uma tampa.

**Como resolver:** apoiar a tampa com o grampo → aliviar a fixação com a chave de boca → recolher a alça → conduzir o carrinho pela abertura → reposicioná-lo na curva do corredor → prender o equipamento à bancada para liberar a bandeja. As folgas e dobradiças contam o problema visualmente.

**Onde fica difícil:** o carrinho ocupa parte da passagem; é preciso deixá-lo num recuo para sacar a arma e combater. O jogador pode preparar esses recuos e o trajeto antes de iniciar o transporte. Não há conta de pesos nem plataformas numeradas.

**Erro recuperável:** encostar numa quina interrompe o movimento. O equipamento não explode, não perde a recompensa e pode ser recuado. Inimigos não o empurram para áreas inacessíveis.

**Protótipo:** um carrinho, alça articulada e dois pontos de parada. Transporte/rotação são trabalho novo; a versão inicial pode usar movimentos guiados, sem criar um sistema geral de arrastar móveis.

## 6. Desembalar o acervo antes de atravessar a segurança

**Local:** bancada de conservação e passagem do atendimento. **Ferramentas:** espátula fina e alicate de precisão. **Referência:** preparação física e escolha de método dos jogos pesquisados; situação autoral de biblioteca.

A recompensa está guardada na capa de um volume protegido por uma presilha. Levar o conjunto pela passagem sinalizada aciona uma resposta local de segurança. O jogador pode extrair somente o que precisa na bancada ou procurar a passagem de serviço e transportar o conjunto intacto.

**Como resolver pela bancada:** fixar a capa no apoio → usar a espátula para expor a lingueta da presilha → segurar a lingueta enquanto retira a haste com o alicate → abrir a capa sem arrancar o suporte → recolher a recompensa. O formato das peças mostra o encaixe e a direção de retirada.

**Onde fica difícil:** o jogador trabalha voltado para a bancada enquanto uma ameaça circula pelo setor. Romper tudo à força é rápido, mas provoca ruído e atrai uma investigação; desmontar permite reduzir a exposição sonora. As ações são curtas e podem ser interrompidas para lutar.

**Erro recuperável:** a presilha retorna ao apoio se a lingueta não estiver sustentada. Tentar atravessar com o conjunto recebe aviso antes da resposta local, e o jogador pode recuar. A chave não é destruída junto com a embalagem.

**Protótipo:** um volume especial, duas peças móveis, dois usos de ferramenta e uma zona de aviso. A segurança é uma regra deste desafio, sem alegar reproduzir o funcionamento real de um equipamento RFID.

## O que testar primeiro

Começar pela **gaveta obstruída**: permite conferir se observar o mecanismo e escolher onde usar a ferramenta realmente produz raciocínio. Acrescentar um inimigo somente depois de a desmontagem funcionar. Se o jogador apenas recolher a ferramenta e apertar E uma vez, o puzzle ainda precisa de trabalho.

Para uma opção com mais ação, testar **a ameaça rompendo a escora**, mantendo a alternativa por ferramenta. Deixar o transporte do equipamento para uma rodada posterior por exigir mais interação física.

Escolher um desafio principal para a chave da biblioteca e, no máximo, uma opção menor para suprimentos ou atalho. As seis ideias são alternativas, sem obrigação de combiná-las na mesma visita.

## Compatibilidade e verificação

As interações acontecem no espaço 3D. Leitura e teclado atuais pausam o gameplay; não servem para simular trabalho manual sob ataque. O player mantém movimentação, arma, vida, munição e chaves existentes. Pegar/largar ferramentas, movimentos guiados e peças destrutíveis são sistemas novos a prototipar, sem presumir que já existem.

Testar alcance das mãos/câmera do player, colisões, recomeço após interrupção, ferramenta largada, morte, checkpoint e retorno ao campus. Os inimigos usam posições/regras de spawn coerentes com o jogo; não aparecem sobre a bancada ou em cima do jogador para aumentar artificialmente a dificuldade.

Nenhuma dessas propostas foi testada em Play Mode ou implementada. Tempos, custos de munição e quantidades de ameaças serão definidos a partir do protótipo escolhido.
