# Proposta — duas chaves da biblioteca e gerador das celas

**Data:** 09/11/2026. **Estado:** ideia de gameplay e roteiro de implementação; nenhuma mudança aplicada ao jogo nesta tarefa.

## Regra definida

- Duas chaves diferentes abrem a entrada da biblioteca. As duas são obrigatórias e podem ser encontradas em qualquer ordem.
- As chaves deixam de servir às celas. Não existe mais uma chave por professor nesta proposta.
- Dentro da biblioteca, o jogador conserta e liga um gerador de emergência.
- O gerador libera as fechaduras das **14 celas**, incluindo a de Bianca.
- O jogador retorna à prisão e abre cada porta com `E`, ouvindo a fala do professor correspondente quando houver gravação.

**Percurso:** explorar Direito e Bloco 4 → conseguir as duas chaves → abrir a biblioteca → resolver o gerador → voltar à prisão → libertar os professores.

A biblioteca considerada é `CampusLibrary`, o interior novo da Unimar. A cena antiga `Library` não faz parte desta proposta.

## Ideia recomendada: “O circuito da custódia”

A universidade possuía um gerador de emergência em um anexo técnico da biblioteca. Uma instalação clandestina desviou parte dessa alimentação para manter o bloqueio elétrico das celas. Com o sistema desligado, as travas de segurança ficaram presas. Restaurar a energia permite acionar o comando de liberação da custódia.

Um bilhete de manutenção apresenta a relação sem entregar a solução inteira:

> “As grades não respondem. O circuito de liberação depende da reserva da biblioteca.”

O gerador deve ficar em um pequeno espaço de manutenção ventilado, com acesso pela área de serviço. Essa localização é uma proposta: o mapa atual ainda precisa ser conferido para acomodar o equipamento e a circulação. Livros, cabines e banheiros mantêm sua função.

## As duas chaves de acesso

| Chave | Local proposto | Como obter | Função |
| --- | --- | --- | --- |
| Chave A — Biblioteca / Administração | Bloco de Direito | Explorar uma sala administrativa acessível antes da biblioteca; uma anotação indica o destino da chave | Primeira fechadura da entrada |
| Chave B — Biblioteca / Reserva técnica | Bloco 4 | Concluir um puzzle curto de tecnologia ou adaptar a recompensa de um puzzle do bloco | Segunda fechadura da entrada |

São sugestões de distribuição. Os pontos exatos devem ser escolhidos na implementação, respeitando os desafios já existentes nesses mapas.

A entrada possui duas fechaduras ou dois indicadores físicos, permitindo entender por que uma chave sozinha não basta. Mensagens sugeridas:

- Sem nenhuma: **“BIBLIOTECA TRANCADA — ENCONTRE AS DUAS CHAVES.”**
- Com uma: **“1/2 CHAVES — FALTA A CHAVE DA ADMINISTRAÇÃO / RESERVA TÉCNICA.”**
- Com as duas: **“[E] DESTRANCAR BIBLIOTECA.”**

As chaves permanecem registradas na sessão. Depois de destrancada, a entrada continua aberta nas próximas visitas. Nenhuma das duas chaves, nem o caminho obrigatório para obtê-las, pode depender de entrar na biblioteca ou abrir uma cela.

## O puzzle do gerador

Meta inicial de duração: **4 a 7 minutos**, a ajustar após testar o percurso.

### 1. Descobrir o problema

Ao chegar, o jogador encontra o painel de emergência sem energia. Um esquema simples liga três símbolos: **gerador → circuito da custódia → fechaduras**. Uma luz vermelha acompanha a mensagem “RESERVA INDISPONÍVEL”.

Tentar ligar revela que falta um fusível. O objetivo passa a ser **“Encontre o fusível da reserva”**.

### 2. Explorar a biblioteca

O fusível fica em uma caixa de manutenção em uma das salas de estudo, fora da sala do gerador. Uma nota no atendimento indica onde o material foi levado.

Na bancada de pesquisa, um manual explica o painel com três símbolos grandes: **motor, alimentação e custódia**. A ordem fica visível no documento e pode ser consultada novamente. As pistas não dependem de adivinhar números nem de conhecer eletricidade.

Assim, o jogador visita atendimento, pesquisa e ala norte, aproveitando os espaços existentes. Para a primeira versão, usar somente um componente coletável; combustível, bateria e ferramentas adicionais ficam como alternativas.

### 3. Preparar o circuito

Depois de instalar o fusível, o jogador ajusta três seletores conforme o esquema encontrado. Um ajuste incorreto produz um estalo e acende o indicador do circuito errado. Permite tentar novamente sem perder o fusível nem reiniciar a exploração.

O painel deve comunicar o erro. Evitar uma sequência secreta que só possa ser descoberta por tentativa e erro.

### 4. Ligar e liberar

Segurar `E` por aproximadamente três segundos liga o motor. Soltar antes interrompe a tentativa e permite repeti-la, sem punição permanente.

O motor estabiliza, algumas luminárias de emergência existentes acendem e o painel mostra **“RESERVA ATIVA”**. Um botão protegido fica disponível: **“LIBERAR CUSTÓDIA”**.

Pressionar esse botão registra a liberação das 14 fechaduras. Um som de contatores e uma mensagem curta confirmam o resultado:

> “CUSTÓDIA LIBERADA — RETORNE ÀS CELAS.”

O gerador fica ligado. Não exigir reabastecimento ou uma nova ativação a cada visita.

## O que acontece nas celas

Antes da liberação: **“CELA SEM LIBERAÇÃO — ATIVE A RESERVA NA BIBLIOTECA.”** As portas continuam fechadas e sólidas.

Depois da liberação: todas podem ser abertas com `E`, sem consultar inventário de chaves. A abertura continua individual, preservando o encontro e a fala de cada professor. Ligar o gerador não deve disparar 14 vozes ao mesmo tempo.

Uma cela aberta permanece aberta ao sair e voltar. A ativação do gerador não transporta professores automaticamente nem encerra o jogo. O acesso à prisão, a passagem após o chefe e o portal para o campus mantêm seus contratos, salvo decisão posterior.

## Alternativas para dar personalidade ao desafio

Escolher inicialmente apenas uma destas variações:

| Ideia | O que muda | Efeito esperado |
| --- | --- | --- |
| **O barulho chama atenção** | A primeira partida alerta um pequeno grupo já colocado na biblioteca | Cria uma saída tensa depois da solução, sem uma luta durante a leitura do manual |
| **Partida instável** | O motor dá uma primeira falha; o painel aponta o seletor que precisa ser corrigido | Pequeno susto e interação adicional, sem espalhar mais itens |
| **Escolha de alimentação** | O painel permite direcionar a reserva à iluminação ou à custódia; a escolha pode ser corrigida | Torna a relação entre biblioteca e prisão visível |
| **Combustível em vez de fusível** | Um galão substitui o componente da versão principal | Objetivo mais imediato e visual; usar um único recurso obrigatório |
| **Liberação por dois circuitos** | Um comando libera as celas 1–7 e outro as 8–14, no mesmo painel | Dá dois momentos de confirmação; sem criar outras chaves |
| **Apagão parcial** | Ao liberar a custódia, algumas luzes do salão apagam e a sinalização de saída permanece acesa | Muda a atmosfera do retorno sem refazer o mapa |

**Recomendação:** fusível + esquema do painel + liberação central. Depois de validar essa base, acrescentar o barulho atraindo atenção. Evitar acumular todas as variações e transformar o objetivo numa sequência longa de buscas.

Se houver inimigos, posicioná-los antes da ativação ou usar os critérios de aparição segura já existentes no projeto. Não fazê-los surgir diante da câmera nem bloquear a única saída.

## Como implementar de forma leve

- Um prefab persistente do gerador, com colisor simples, material reutilizado, painel e poucos indicadores emissivos.
- Interações por proximidade e `E`, aproveitando o padrão do jogo.
- Uma animação curta de partida e vibração discreta apenas enquanto o equipamento estiver próximo/visível.
- Um áudio de partida e um loop 3D com alcance limitado. O loop termina ao descarregar a biblioteca; o estado de progresso permanece.
- Alternar algumas luzes existentes em vez de adicionar várias luzes dinâmicas com sombras.
- Atualizar objetivos, painel e portas por mudança de estado. Não procurar todas as celas a cada frame nem manter a biblioteca carregada quando o jogador estiver no porão.

## Estados e integração com o código atual

Estados propostos: **sem fusível → fusível instalado → circuito preparado → motor ligado → custódia liberada**. Registrar também as duas chaves coletadas e a entrada da biblioteca destrancada.

A liberação deve persistir entre cenas e recargas de checkpoint da mesma sessão, seguindo o contrato atual de `PrisonProgress`. Um novo jogo limpa esse progresso. Persistência após fechar o aplicativo exige uma decisão separada sobre salvamento.

Mudanças previstas para uma futura implementação:

1. Criar dois identificadores próprios de acesso à biblioteca, sem reutilizar IDs `professor-cell-XX` com outro significado.
2. Colocar as duas chaves fora da biblioteca e atualizar a entrada de `CampusExterior` para exigir ambas. A saída deve permanecer utilizável após entrar.
3. Substituir a condição de chave em `PrisonProgress.CanOpenCell` pelo estado de custódia liberada. Manter o registro de portas abertas por ID.
4. Atualizar as mensagens e retirar da HUD das celas a lista de chaves individuais. Mostrar o objetivo do gerador e, se desejado, o contador de professores libertados.
5. Retirar da progressão os 13 pickups de chave de cela atuais e adaptar as recompensas dos puzzles afetados. As cinco chaves hoje colocadas na biblioteca deixam de ser objetivos. Não exigir criar uma chave 14 para Bianca.
6. Revisar a proposta anterior de caixas com senhas da biblioteca: as pistas podem servir ao gerador, mas abrir caixas para obter chaves de cela deixa de fazer sentido.
7. Desligar o bypass temporário `AllowKeylessCellOpeningForLayoutReview` na validação da regra definitiva, para ele não esconder falhas no bloqueio do gerador.
8. Conferir dependências antes de retirar referências antigas. Sprites, prefabs compartilhados e demais assets não devem ser excluídos automaticamente.

O gerador e seus assets exclusivos ficam em `Assets/_Project/Scenes/CampusLibrary/`; somente o estado compartilhado entre mapas pertence ao sistema global ou à progressão da prisão. Posição final, medidas e novos assets precisam ser conferidos no mapa antes da implementação.

## Critérios para considerar a mecânica pronta

- Uma chave sozinha não abre a biblioteca; as duas funcionam em qualquer ordem.
- Não existe dependência circular entre biblioteca, chaves, fusível e celas.
- Nenhuma cela abre antes da liberação; depois dela, as 14 abrem sem chave individual.
- O gerador, a entrada e as celas mantêm seus estados ao trocar de cena e recarregar checkpoint.
- Um novo jogo restaura o estado inicial.
- As falas continuam exclusivas de cada professor e tocam somente na interação correspondente.
- Não há nova onda, nova recompensa ou reinício do puzzle ao retornar à biblioteca.
- O painel, o caminho de retorno e as mensagens ficam legíveis no jogo.
- Compilação, Play Mode, transições e custo de renderização são verificados durante a implementação.

## Contexto consultado

- [Ficha da biblioteca](CampusLibrary.md).
- [Progressão anterior das chaves](PrisonCellKeys.md).
- [Proposta anterior de puzzles de leitura](CampusLibrary-Puzzles-Leitura.md).
- [Estado temporário e pendências das celas](Basement-Celas-Pendencias.md).
- `PrisonProgress.cs`, `PrisonCellGate.cs` e `PrisonController.cs`, conferidos no projeto.

Este documento registra a nova direção proposta pelo usuário. Os arquivos de gameplay e as fichas que descrevem o comportamento atual ainda não foram migrados.
