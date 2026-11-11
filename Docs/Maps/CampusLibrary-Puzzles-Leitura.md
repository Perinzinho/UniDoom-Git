# Biblioteca — pistas espalhadas para abrir as caixas das chaves

**Proposta para avaliação; ainda não implementada.** A estrutura é parecida com o puzzle do Bloco 4: o jogador encontra informações em diferentes pontos do mapa e combina essas informações na ordem mostrada junto à recompensa.

Cada uma das cinco chaves fica numa caixa com senha de três números. Na tampa aparecem três assuntos. Os números correspondentes estão espalhados pela biblioteca, em livros e anotações.

## O que o jogador precisa entender

Um aviso no atendimento explica a regra:

> “As caixas usam os números dos marcadores. Siga a ordem dos assuntos indicada na tampa.”

Assim, o desafio é explorar a biblioteca, encontrar os marcadores e relacionar assunto e número. A ordem de cada senha fica visível na própria caixa.

## Cinco pistas em lugares diferentes

| Pista encontrada | Onde procurar |
|---|---|
| História = 7 | Ficha de um livro no balcão de atendimento |
| Biologia = 2 | Marcador dentro de um livro na bancada de pesquisa |
| Tecnologia = 9 | Anotação ao lado de um computador na área de consulta |
| Literatura = 4 | Livro aberto em uma das salas de estudo ao fundo |
| Geografia = 6 | Marcador em uma cabine individual da parede norte |

As pistas aparecem como informações claras: o assunto e um único número em destaque. Cada ponto fica em uma área diferente, fazendo o jogador atravessar a biblioteca e visitar a ala do fundo.

## Cada chave usa uma combinação

As caixas ficam nos pontos das chaves já distribuídas. Cada uma pede três assuntos em uma ordem própria:

| Chave | Local da caixa | Ordem na tampa | Senha de exemplo |
|---|---|---|---|
| 4 | Mesa de estudo individual | História → Biologia → Literatura | 724 |
| 5 | Mesa de leitura em carvalho | Literatura → Geografia → História | 467 |
| 6 | Bancada de pesquisa | Tecnologia → História → Biologia | 972 |
| 10 | Cabine individual ao norte | Geografia → Tecnologia → Literatura | 694 |
| 11 | Mesa da leitura silenciosa | Biologia → Literatura → Geografia | 246 |

## Exemplo do percurso

O jogador encontra a caixa da chave 6 na bancada de pesquisa. Na tampa está escrito **Tecnologia → História → Biologia**.

Ele precisa consultar a anotação do computador, passar pelo atendimento para localizar a ficha de História e examinar o livro de Biologia na bancada. Ao reunir **9, 7 e 2**, volta à caixa e digita **972** para pegar a chave.

Se já tiver encontrado alguma dessas pistas enquanto explorava, pode aproveitá-la. As mesmas cinco relações servem para todas as caixas; cada caixa muda a combinação e a ordem.

## Dificuldade e circulação

- Para abrir a primeira caixa, o jogador precisa buscar pistas em três pontos diferentes.
- As pistas ficam disponíveis desde a entrada, inclusive nas salas do fundo.
- Cada chave pode ser obtida independentemente das outras; nenhuma pista fica atrás de uma dessas caixas.
- A distribuição das pistas permanece fixa durante a visita, permitindo voltar para consultar uma informação.
- As caixas também ficam espalhadas, mantendo a necessidade de andar depois de descobrir os números.
- Errar a senha permite tentar de novo. Abrir e coletar continuam salvos ao sair e voltar.

O raciocínio necessário é reconhecer a relação entre assunto e número e respeitar a ordem da tampa. A exploração do mapa é a parte principal do desafio.

## Estado atual

Este MD substitui a proposta anterior. Os puzzles ainda não foram implementados: as chaves continuam como pickups comuns até a aprovação da ideia e uma solicitação de implementação. Nenhuma cena, script ou prefab foi alterado nesta revisão do documento.
