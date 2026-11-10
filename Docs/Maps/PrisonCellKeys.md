# Chaves numeradas das celas

Atualizado em 09/11/2026. **Oito chaves de cela retiradas de Direito e Bloco 4** para dar lugar a duas chaves de acesso à biblioteca. As cinco chaves de cela já existentes na biblioteca continuam ativas. A manutenção permanece separada. O gerador que abrirá as celas é trabalho futuro, ainda não implementado. [Contrato de acesso](CampusLibrary.md#acesso-com-duas-chaves--09112026). Direito utiliza sua cena independente; Biblioteca e Bloco 4 utilizam seus prefabs. O porão carrega `BasementScenario.prefab`, que instancia `Basement.prefab`.

| Chave/cela | Professor | Local da chave |
|---|---|---|
| 1 | Caio | Retirada do gameplay em 09/11; abertura futura pelo gerador da biblioteca |
| 2 | Fabio | Retirada do gameplay em 09/11; abertura futura pelo gerador da biblioteca |
| 3 | Henrique | Retirada do gameplay em 09/11; abertura futura pelo gerador da biblioteca |
| 4 | Santi | Biblioteca: mesa de estudo individual |
| 5 | Navarro | Biblioteca: mesa de leitura em carvalho |
| 6 | Matheus Ferraroni | Biblioteca: bancada de pesquisa |
| 7 | Paulinho | Retirada do gameplay em 09/11; abertura futura pelo gerador da biblioteca |
| 8 | Rafa | Retirada do gameplay em 09/11; abertura futura pelo gerador da biblioteca |
| 9 | Gustavo Camossi | Retirada do gameplay em 09/11; abertura futura pelo gerador da biblioteca |
| 10 | Matheus Santi | Biblioteca: cabine individual ao norte |
| 11 | Moacyr | Biblioteca: mesa da leitura silenciosa ao norte |
| 12 | Querino | Retirada do gameplay em 09/11; abertura futura pelo gerador da biblioteca |
| 13 | William | Retirada do gameplay em 09/11; abertura futura pelo gerador da biblioteca |

Inventário existente `PlayerCellKeys`, IDs `professor-cell-01` até `professor-cell-13`. `PrisonCellGate` exige exclusivamente sua chave. `PrisonProgress.CellCount = 13` define os limites do inventário, HUD e componentes. Não há chave universal ativa no porão. Os pickups originais do alarme/puzzle agora concedem somente as duas chaves de acesso à biblioteca, com IDs independentes; os seis pickups adicionais de cela nos blocos ficaram inativos. As oito celas correspondentes permanecem sem nova forma de abertura até a implementação do gerador. As cinco chaves da biblioteca usam `CellKeyPickup`. Coleta e retorno ao mapa consultam o inventário persistente da sessão. Não foi criado outro sistema de save.

Sprites: todas as treze chaves usam o modelo aprovado de metal envelhecido com numeração gravada. Atlas compartilhados `Assets/_Project/Global/Resources/Textures/Pickups/CellKeys.png`, oito subassets `CellKey01`…`CellKey08`, e `CellKeys09-13.png`, cinco subassets `CellKey09`…`CellKey13`. Renderizadores usam o material de sprite URP e Billboard existentes. Fonte, geração, hashes e recortes em `Licenses/AssetSources/CellKeys/generated-sprites.json` e `generated-sprites-09-13.json`. Atlas original e seus oito IDs preservados.

As chaves 9–13 substituem a arte com plaqueta azul por sprites próprios no mesmo estilo das primeiras oito; sem números TextMeshPro sobrepostos. `CellKeySprites` carrega os dois atlas para a HUD da prisão, incluindo as chaves 9–13. A chave 1 da cena independente de Direito também referencia o sprite numerado correto. Altura visual padronizada em aproximadamente 24 cm, considerando a escala dos pais, sem mudar a posição das chaves.

Coleta sobre mesas: alcance horizontal dos triggers aumentado para aproximadamente **1,05 m** em mundo. Pickups comuns e chave 7 usam `SphereCollider` com raio de 1,05 m; chave 1 mantém `BoxCollider`, agora com 2,10 × 1,60 × 2,10 m em mundo. Centro elevado em 12 cm em relação à raiz. Escalas das hierarquias compensadas nos valores locais. Triggers não bloqueiam movimento; tamanho da imagem e área de coleta são independentes. Os checks de porta totalmente aberta/puzzle da 7 foram preservados, assim como inventário, IDs, distribuição e manutenção. O disparo do alarme de Direito segue o contrato atualizado abaixo.

Conferência histórica, anterior à retirada de 09/11: treze pickups ativos com IDs 1–13, imagens transparentes, correspondência dos treze sprites, referências locais e fileIDs preservados, escalas e triggers conferidos. Cópias antigas inativas continuam inativas. Sem abrir Unity, compilar, executar jogo/Play Mode, gerar capturas ou fazer commit/push. A coleta efetiva junto às mesas precisa de teste pelo usuário.

Onze gravações existentes tratadas, mantendo caminhos, GUIDs, importadores e associações. Ruído moderado reduzido, silêncios retirados, volume medido entre −18,30 e −17,98 LUFS e pico verdadeiro máximo −1,50 dBTP. Cortes confirmados pelo usuário: Henrique sem “calma”, mantendo “Vai Corinthians”; Bianca somente “yipeee” final. Medições e hashes em `Licenses/AssetSources/Teachers/audio-edit.json`. Os professores Ferraroni, Navarro e Rafa já não tinham áudio associado; nenhum áudio de outro professor foi atribuído a eles. Todos os onze arquivos fornecidos foram processados, inclusive os ainda sem consumidor.

Validação: matriz 8 × 8 de identidade, duplicatas, IDs inválidos e sprites carregados em objetos isolados no modo de edição; prefabs reabertos, referências e IDs preservados; previews fora de tela em `output/Campus-Ultimas-Chave-*.png`; hashes e métricas dos áudios conferidos. Sem executar jogo, Play Mode, ativar/focar janela ou testar coleta, retorno entre mapas, áudio/HUD em runtime, build ou FPS.

## Chave da manutenção de Direito

`law-school-maintenance` abre apenas a manutenção 104. Após a alteração de acesso em 09/11, Direito contém uma chave da biblioteca e manutenção; Bloco 4 contém uma chave da biblioteca; apenas 4/5/6/10/11 continuam como chaves de cela, dentro da biblioteca. A antiga distribuição equilibrada 5/5/4 foi substituída por este pedido.

Acesso à biblioteca e manutenção aparecem desde a entrada, em salas diferentes fora do corredor da manutenção, com separação horizontal mínima de 45 m. As salas 110/121/132 estão reservadas às três chaves agora retiradas e foram excluídas de ambos os sorteios, evitando chaves na mesma sala. A porta exige a chave específica tanto na lógica de contato quanto no bloqueio sólido do vão. A primeira chave coletada em Direito dispara o alarme: acesso à biblioteca ou manutenção. O evento só inicia uma vez por sessão. O quadro exige a chave da manutenção; depois de desligado, as próximas chaves não reativam o alarme, inclusive após sair e retornar ao mapa.

Os puzzles da biblioteca permanecem como proposta: [cinco puzzles de leitura](CampusLibrary-Puzzles-Leitura.md). Os pickups ainda não estão bloqueados por esses enigmas.

Conferência desta rodada por arquivos e referências. Sem iniciar Unity, jogo, Play Mode, testes, build, capturas, commit ou push.
