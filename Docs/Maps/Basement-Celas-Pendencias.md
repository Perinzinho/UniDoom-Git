# Celas do porão — decisões e pendências

Atualizado em **11/11/2026**. A regra vigente é a liberação das 14 celas pela energia do gerador da CampusLibrary.

> **Conciliação após integrar development:** a retirada das oito chaves de cela de Direito/Bloco 4 e as duas chaves de acesso à biblioteca foram preservadas. Permanecem cinco pickups de cela na biblioteca (4/5/6/10/11), além dos dois de acesso e da manutenção. O gerador continua como implementação futura. A distribuição 5/5/4 e as alternativas de chaves individuais abaixo são propostas/histórico da revisão recebida, não restauração dos pickups retirados. Estado vigente: [PrisonCellKeys.md](PrisonCellKeys.md).

## Regra atual

- As **14 celas começam fechadas**. Antes de reparar o gerador, o aviso indica `CELA XX TRANCADA — REPARE O GERADOR DA BIBLIOTECA`.
- `PrisonProgress.CanOpenCell` exige um ID de 1 a 14 e `LibraryMissionProgress.IsLibraryGeneratorPowered`. Chaves numeradas, vitória sobre o boss e o antigo modo de revisão não liberam uma cela sem energia.
- O prefab e o estado inicial permanecem fechados; a porta só gira depois da interação do jogador.
- Os 13 sprites e o contrato dos IDs existentes continuam preservados; apenas os cinco pickups de cela da biblioteca permanecem ativos. A cela 14 de Bianca possui ID de gate/progresso, sprite de personagem e áudio, mas ainda não possui chave 14, pickup ou sprite de chave. Nenhuma chave é consumida ou concedida automaticamente.
- Depois dos três fusíveis instalados e do puzzle concluído, cada cela aceita `[E] ABRIR CELA XX`. `PrisonProgress.OpenCell` repete a condição de energia antes de registrar o resgate.
- A estrutura principal das 14 celas usa as mesmas medidas, escalas e colisores. Seis grupos antigos de objetos importados, presentes somente nas celas 1–6, foram desativados e preservados para remover a diferença estrutural aparente.
- Abrir a 14ª cela agora conclui o resgate: todos os sprites dos professores somem no mesmo frame e o prompt padronizado das celas passa a mostrar, no centro da tela, `Uma surpresa te espera! Retorne para Unimar`. A próxima travessia do portal do porão para `CampusExterior` inicia a sequência final.
- O final posiciona os 14 professores na chegada da portaria UNIMAR, faz o grupo saltar diante de fogos procedurais, escurece a tela e inicia os créditos compartilhados. O menu principal contém `TESTAR FINAL`, que prepara o gerador pelo fluxo real de progresso em uma sessão limpa e inicia na prisão após o boss, com as 14 celas fechadas.

## Propostas históricas sobre quantidade de chaves

As propostas e listas abaixo registram o planejamento anterior à decisão de 11/11. A condição vigente de abertura é o gerador reparado, descrita acima.

Na proposta histórica de **13 chaves de cela** para 14 professores/celas, seria necessário criar uma chave 14 individual para Bianca ou definir outra regra. Essa proposta deve ser reconsiderada junto ao gerador futuro antes de reativar as fechaduras; não altera o estado vigente de cinco pickups de cela ativos.

Distribuição histórica das 13 chaves, anterior à conversão dos blocos:

| Mapa | Celas/chaves | Quantidade |
|---|---|---:|
| Direito | 1, 2, 3 e 9 | 4 |
| Biblioteca | 4, 5, 6, 10 e 11 | 5 |
| Bloco 4 | 7, 8, 12 e 13 | 4 |
| **Total** | **1–13** | **13** |

Direito também tinha uma chave separada da manutenção. Aquele inventário somava **14 pickups de chave relacionados à progressão**; a configuração vigente contém cinco pickups de cela, dois de acesso à biblioteca e manutenção.

Alternativas que exigiriam reestruturação:

- **3 chaves por setor/mapa:** reduz procura, mas elimina a associação individual entre chave e professor.
- **1 chave mestra:** simplifica demais a exploração e torna sprites, HUD e pickups numerados redundantes.
- **Resgate por grupos:** exige definir quais celas compartilham chave, refazer mensagens da HUD e decidir como registrar grupos parcialmente abertos.

Antes de escolher uma alternativa, confirmar se libertar cada professor será objetivo obrigatório, opcional ou parte de finais diferentes.

## Lógica proposta para as salas

1. Cada sala escolhida nos três mapas contém no máximo uma chave de cela.
2. A chave deve ser obtida após uma ação legível: exploração, puzzle, combate ou evento próprio da sala.
3. Ao coletar, `PlayerCellKeys` registra `professor-cell-XX` e mantém o item durante as trocas de cena da sessão.
4. No porão, a cela `XX` consulta exclusivamente esse ID. Sem a chave, mostra a mensagem de trancada; com a chave, permite abrir e registra a cela em `PrisonProgress`.
5. A abertura toca a fala do professor quando houver áudio. Navarro e Matheus Ferraroni continuam sem fala até os arquivos corretos serem fornecidos.
6. Deve ser decidido o efeito do resgate: somente diálogo, contador de resgatados, requisito para saída/final, recompensa ou consequência narrativa.

Pendências de design das salas:

- [ ] Decidir e implementar a chave 14 de Bianca, incluindo sprite, pickup e sala de origem.
- [ ] Confirmar se as 14 celas são obrigatórias ou opcionais.
- [x] Definir a condição final: todas as 14 celas abertas.
- [ ] Definir a ação que libera cada pickup; hoje várias chaves estão apenas colocadas sobre mesas.
- [ ] Revisar as cinco propostas de puzzle da Biblioteca e escolher quais serão implementadas.
- [ ] Confirmar se o alarme de Direito deve continuar disparando com a primeira chave relevante coletada.
- [ ] Confirmar se a chave 7 continuará vinculada ao puzzle da sala 423 do Bloco 4.
- [x] Definir feedback de conclusão após o último resgate e retorno à UNIMAR.
- [ ] Decidir se o progresso precisa sobreviver apenas à sessão atual ou também a salvar/carregar o jogo.

## Validação da liberação pelo gerador

- [x] Remover as permissões temporárias de revisão e vincular autorização/registro de abertura ao estado real de energia.
- [x] Verificação isolada dos métodos reais de progresso e abertura: 18 estados bloqueados × 14 celas, inclusive com chaves antigas e boss derrotado.
- [x] Verificar conclusão do puzzle antes do acionamento: `PuzzleSolved` ainda bloqueia; `GeneratorOn` libera os IDs 1–14.
- [x] Verificar IDs inválidos, jogador morto, pausa/menu, chamadas diretas e abertura repetida em objetos substitutos.
- [x] Verificar restauração das celas abertas, resgate após 14 aberturas, reset de partida e preparação do atalho `TESTAR FINAL` em verificação isolada.
- [x] Compilar C# do runtime com as referências locais da Unity, sem iniciar o Editor; avisos preexistentes.
- [ ] Teste manual em jogo: chegar à prisão antes/depois do reparo, conferir colisão/aviso, falas, retorno entre mapas e checkpoint.
- [ ] Play Mode, build e comparação visual pelo usuário.

## Arquivos afetados se o contrato mudar

- `Assets/_Project/Scenes/Basement/Prison/Scripts/PrisonProgress.cs`: quantidade, IDs e estado das celas.
- `Assets/_Project/Scenes/Basement/Prison/Scripts/PrisonCellGate.cs`: requisito e abertura individual.
- `Assets/_Project/Scenes/Basement/Prison/Scripts/PrisonController.cs`: interação, mensagens, HUD e áudio.
- `Assets/_Project/Global/Scripts/Player/PlayerCellKeys.cs`: inventário persistente da sessão.
- `Assets/_Project/Global/Scripts/Pickups/CellKeyPickup.cs` e `CellKeySprites.cs`: coleta e sprites.
- `Assets/_Project/Scenes/Basement/Prefabs/Basement.prefab`: 14 portas e professores.
- Prefabs/cenas de Direito, Biblioteca e Bloco 4: localização e regras dos pickups.
- `Docs/Maps/PrisonCellKeys.md` e fichas dos mapas: contrato e distribuição final.
