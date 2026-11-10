# Celas do porão — decisões e pendências

Atualizado em **09/11/2026**. Este documento acompanha o estado temporário das celas e as decisões necessárias antes de fechar a progressão definitiva.

> **Conciliação após integrar development:** a retirada das oito chaves de cela de Direito/Bloco 4 e as duas chaves de acesso à biblioteca foram preservadas. Permanecem cinco pickups de cela na biblioteca (4/5/6/10/11), além dos dois de acesso e da manutenção. O gerador continua como implementação futura. A distribuição 5/5/4 e as alternativas de chaves individuais abaixo são propostas/histórico da revisão recebida, não restauração dos pickups retirados. Estado vigente: [PrisonCellKeys.md](PrisonCellKeys.md).

## Estado temporário atual

- As **14 celas começam fechadas**, mostram `[E] ABRIR CELA XX` e podem ser abertas sem chave para permitir revisão de layout, professores, circulação, áudio e leitura visual.
- O acesso temporário é controlado somente por `PrisonProgress.AllowKeylessCellOpeningForLayoutReview = true`.
- O prefab e o estado inicial permanecem fechados; a porta só gira depois da interação do jogador.
- Os 13 sprites e o contrato dos IDs existentes continuam preservados; apenas os cinco pickups de cela da biblioteca permanecem ativos. A cela 14 de Bianca possui ID de gate/progresso, sprite de personagem e áudio, mas ainda não possui chave 14, pickup ou sprite de chave. Nenhuma chave é consumida ou concedida automaticamente.
- Para voltar ao comportamento trancado, alterar a constante para `false`, compilar e executar a validação indicada no fim deste documento.
- A estrutura principal das 14 celas usa as mesmas medidas, escalas e colisores. Seis grupos antigos de objetos importados, presentes somente nas celas 1–6, foram desativados e preservados para remover a diferença estrutural aparente.

## Decisão recomendada: quantidade de chaves

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
- [ ] Definir a condição final: uma cela, quantidade mínima ou todas as 14.
- [ ] Definir a ação que libera cada pickup; hoje várias chaves estão apenas colocadas sobre mesas.
- [ ] Revisar as cinco propostas de puzzle da Biblioteca e escolher quais serão implementadas.
- [ ] Confirmar se o alarme de Direito deve continuar disparando com a primeira chave relevante coletada.
- [ ] Confirmar se a chave 7 continuará vinculada ao puzzle da sala 423 do Bloco 4.
- [ ] Definir feedback após cada resgate e um resumo de professores libertados.
- [ ] Decidir se o progresso precisa sobreviver apenas à sessão atual ou também a salvar/carregar o jogo.

## Para trancar as celas novamente

- [ ] Alterar `PrisonProgress.AllowKeylessCellOpeningForLayoutReview` para `false`.
- [x] Confirmar as 14 instâncias de `PrisonCellGate`, IDs únicos de 1 a 14 e referências corretas de professor.
- [ ] Criar o pickup da chave 14 e então confirmar os 14 pickups ativos e a correspondência entre chave, sprite, sala e cela.
- [ ] Verificar que nenhuma sala recebe duas chaves e que nenhuma chave fica inacessível antes do próprio requisito.
- [ ] Testar uma cela sem chave: colisão sólida, áudio de porta trancada e mensagem correta.
- [ ] Testar uma cela com a chave correta e com uma chave incorreta.
- [ ] Testar saída/retorno ao porão e recarga de checkpoint após abrir celas.
- [ ] Testar HUD com 0, 1, 13 e 14 chaves após criar a chave de Bianca.
- [ ] Testar falas, professores sem áudio e múltiplas celas abertas em sequência.
- [ ] Executar Play Mode, build e comparação visual antes de considerar a progressão concluída.

## Arquivos afetados se o contrato mudar

- `Assets/_Project/Scenes/Basement/Prison/Scripts/PrisonProgress.cs`: quantidade, IDs e estado das celas.
- `Assets/_Project/Scenes/Basement/Prison/Scripts/PrisonCellGate.cs`: requisito e abertura individual.
- `Assets/_Project/Scenes/Basement/Prison/Scripts/PrisonController.cs`: interação, mensagens, HUD e áudio.
- `Assets/_Project/Global/Scripts/Player/PlayerCellKeys.cs`: inventário persistente da sessão.
- `Assets/_Project/Global/Scripts/Pickups/CellKeyPickup.cs` e `CellKeySprites.cs`: coleta e sprites.
- `Assets/_Project/Scenes/Basement/Prefabs/Basement.prefab`: 14 portas e professores.
- Prefabs/cenas de Direito, Biblioteca e Bloco 4: localização e regras dos pickups.
- `Docs/Maps/PrisonCellKeys.md` e fichas dos mapas: contrato e distribuição final.
