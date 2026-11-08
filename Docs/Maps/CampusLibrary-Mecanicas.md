# Mecânicas — Biblioteca da Unimar

O banco anterior de ideias e puzzles foi removido a pedido do usuário. A direção atual é **interação física com ferramentas, cenário e perigo**, inspirada em Amnesia e Resident Evil, adaptada à biblioteca e à continuidade de UniDoom.

As novas propostas estão somente em [Puzzles físicos sob pressão](CampusLibrary-Puzzles-Caoticos.md). São ideias para escolha, sem implementação ou aprovação de um puzzle específico.

## O que o jogo já oferece

| Base conferida em código e prefabs | Como aproveitar |
| --- | --- |
| Player persistente, arma, vida, munição e interação | Manter os controles e permitir interromper o trabalho para combater |
| Enemy investiga tiros elegíveis pela origem do disparo, com alcance e oclusão | Considerar o custo de usar a arma para romper obstáculos ou preparar uma posição; não tratar a reação como rastreamento automático |
| ClassRoom tem coleta de Key01 e reforços no retorno | Variar a tarefa executada no espaço, além da coleta |
| A biblioteca antiga já tem Key02, apagão e ondas | Dar à nova biblioteca uma ação física própria |
| Direito tem chave, alarme, reforços e quadro que encerra a emergência | Preferir consequências locais dos objetos a outra sirene com cerco do prédio |
| Bloco 4 usa fotografias/símbolos e teclado | Usar a observação do cenário para entender mecanismos físicos |
| GameMenus pausa leitura e teclado | As tarefas executadas sob ataque precisam continuar no mundo 3D |

Fontes locais: [ClassRoom](../../Assets/_Project/Scenes/ClassRoom/Scripts/ClassroomEncounterDirector.cs), [biblioteca antiga](../../Assets/_Project/Scenes/Library/Scripts/EncounterRevision/LibraryEncounterDirector.cs), [Enemy](../../Assets/_Project/Global/Scripts/Enemy/Enemy.cs), [documentos](../../Assets/_Project/Global/Scripts/UI/GameMenus.Document.cs), [campus](../../README_CAMPUS_EXTERIOR.md) e [Direito](LawSchool.md).

## Objetivo e limitações atuais

O contexto de [ideias de Direito](LawSchool-Ideias-de-Mecanicas.md) define chaves individuais para celas de professores. Uma recompensa de CampusLibrary deve ter identidade e destino definidos na campanha. Essa integração ainda está incompleta: PrisonCellGate consulta o estado geral PrisonProgress.HasKey, enquanto PlayerCellKeys guarda identificadores individuais e a chave do Bloco 4 ainda não está ligada a uma cela. Não atribuir uma cela definitiva à biblioteca sem resolver esse contrato.

As cenas do campus podem ser visitadas em ordens diferentes; a ordem de Build Settings não define, por si só, a cronologia do jogador. Um puzzle da biblioteca deve ser compreensível sem depender de uma visita anterior a Direito ou Bloco 4.

O espaço disponível inclui atendimento, acervo, consulta, mesas abertas, duas salas coletivas envidraçadas e posições individuais. As propostas priorizam esses setores. Manipulação livre, transporte e ataques ao cenário são trabalho novo quando usados; a existência de modelos 3D de ferramentas não prova que exista um sistema de uso dessas ferramentas.

## Referências da nova direção

- [Amnesia: The Bunker — apresentação da Frictional Games](https://store.steampowered.com/app/1944430/Amnesia_The_Bunker/): ferramentas e diferentes abordagens de sobrevivência.
- [Resident Evil 7 — Bedroom](https://www.gamesradar.com/how-to-escape-the-bedroom-in-resident-evil-7s-new-dlc/): alteração e recomposição de um espaço sob ameaça.
- [Penumbra: Overture — apresentação oficial](https://store.steampowered.com/app/22180/Penumbra_Overture/): objetos e mecanismos manipuláveis.

As fontes e a adaptação de cada ideia estão no MD específico. Pesquisa documental em 07/11/2026; nenhuma alegação de teste desses jogos ou de implementação em UniDoom.
