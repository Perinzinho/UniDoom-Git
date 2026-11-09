# Chaves numeradas das celas

Atualizado em 09/11/2026. Treze chaves de cela e uma chave de manutenção, distribuídas em 5/5/4 por prédio. Direito utiliza sua cena independente; Biblioteca e Bloco 4 utilizam seus prefabs. O porão carrega `BasementScenario.prefab`, que instancia `Basement.prefab`.

| Chave/cela | Professor | Local da chave |
|---|---|---|
| 1 | Caio | Direito: chave original sorteada entre 33 ambientes, com alarme original |
| 2 | Fabio | Direito: mesa docente da sala 110 |
| 3 | Henrique | Direito: mesa docente da sala 121 |
| 4 | Santi | Biblioteca: mesa de estudo individual |
| 5 | Navarro | Biblioteca: mesa de leitura em carvalho |
| 6 | Matheus Ferraroni | Biblioteca: bancada de pesquisa |
| 7 | Paulinho | Bloco 4: chave original do puzzle na bancada da 423 |
| 8 | Rafa | Bloco 4: mesa docente da 428 |
| 9 | Gustavo Camossi | Direito: mesa docente da sala 132 |
| 10 | Matheus Santi | Biblioteca: cabine individual ao norte |
| 11 | Moacyr | Biblioteca: mesa da leitura silenciosa ao norte |
| 12 | Querino | Bloco 4: mesa docente da sala 414 |
| 13 | William | Bloco 4: mesa docente da sala 421 |

Inventário existente `PlayerCellKeys`, IDs `professor-cell-01` até `professor-cell-13`. `PrisonCellGate` exige exclusivamente sua chave. `PrisonProgress.CellCount = 13` define os limites do inventário, HUD e componentes. Não há chave universal ativa no porão. Os pickups originais do alarme/puzzle continuam responsáveis pelas chaves 1/7; os demais usam `CellKeyPickup`. Coleta e retorno ao mapa consultam o inventário persistente da sessão. Não foi criado outro sistema de save.

Sprites: todas as treze chaves usam o modelo aprovado de metal envelhecido com numeração gravada. Atlas compartilhados `Assets/_Project/Global/Resources/Textures/Pickups/CellKeys.png`, oito subassets `CellKey01`…`CellKey08`, e `CellKeys09-13.png`, cinco subassets `CellKey09`…`CellKey13`. Renderizadores usam o material de sprite URP e Billboard existentes. Fonte, geração, hashes e recortes em `Licenses/AssetSources/CellKeys/generated-sprites.json` e `generated-sprites-09-13.json`. Atlas original e seus oito IDs preservados.

As chaves 9–13 substituem a arte com plaqueta azul por sprites próprios no mesmo estilo das primeiras oito; sem números TextMeshPro sobrepostos. `CellKeySprites` carrega os dois atlas para a HUD da prisão, incluindo as chaves 9–13. A chave 1 da cena independente de Direito também referencia o sprite numerado correto. Altura visual padronizada em aproximadamente 24 cm, considerando a escala dos pais, sem mudar a posição das chaves.

Coleta sobre mesas: alcance horizontal dos triggers aumentado para aproximadamente **1,05 m** em mundo. Pickups comuns e chave 7 usam `SphereCollider` com raio de 1,05 m; chave 1 mantém `BoxCollider`, agora com 2,10 × 1,60 × 2,10 m em mundo. Centro elevado em 12 cm em relação à raiz. Escalas das hierarquias compensadas nos valores locais. Triggers não bloqueiam movimento; tamanho da imagem e área de coleta são independentes. Os checks de porta totalmente aberta/puzzle da 7 foram preservados, assim como inventário, IDs, distribuição e manutenção. O disparo do alarme de Direito segue o contrato atualizado abaixo.

Conferência desta alteração por arquivos: treze pickups ativos com IDs 1–13, imagens transparentes, correspondência dos treze sprites, referências locais e fileIDs preservados, escalas e triggers conferidos. Cópias antigas inativas continuam inativas. Sem abrir Unity, compilar, executar jogo/Play Mode, gerar capturas ou fazer commit/push. A coleta efetiva junto às mesas precisa de teste pelo usuário.

Onze gravações existentes tratadas, mantendo caminhos, GUIDs, importadores e associações. Ruído moderado reduzido, silêncios retirados, volume medido entre −18,30 e −17,98 LUFS e pico verdadeiro máximo −1,50 dBTP. Cortes confirmados pelo usuário: Henrique sem “calma”, mantendo “Vai Corinthians”; Bianca somente “yipeee” final. Medições e hashes em `Licenses/AssetSources/Teachers/audio-edit.json`. Os professores Ferraroni, Navarro e Rafa já não tinham áudio associado; nenhum áudio de outro professor foi atribuído a eles. Todos os onze arquivos fornecidos foram processados, inclusive os ainda sem consumidor.

Validação: matriz 8 × 8 de identidade, duplicatas, IDs inválidos e sprites carregados em objetos isolados no modo de edição; prefabs reabertos, referências e IDs preservados; previews fora de tela em `output/Campus-Ultimas-Chave-*.png`; hashes e métricas dos áudios conferidos. Sem executar jogo, Play Mode, ativar/focar janela ou testar coleta, retorno entre mapas, áudio/HUD em runtime, build ou FPS.

## Chave da manutenção de Direito

`law-school-maintenance` abre apenas a manutenção 104. Após o pedido de distribuição equilibrada, Direito contém as celas 1/2/3/9 e manutenção: cinco chaves. Biblioteca contém 4/5/6/10/11: cinco. Bloco 4 contém 7/8/12/13: quatro. Este pedido substitui a limitação anterior de duas chaves em Direito.

Cela 1 e manutenção aparecem desde a entrada, em salas diferentes fora do corredor da manutenção, com separação horizontal mínima de 45 m. As salas 110/121/132 estão reservadas às três outras chaves e foram excluídas de ambos os sorteios, evitando chaves na mesma sala. A porta exige a chave específica tanto na lógica de contato quanto no bloqueio sólido do vão. A primeira das cinco chaves coletada em Direito dispara o alarme: celas 1/2/3/9 ou manutenção. O evento só inicia uma vez por sessão. O quadro exige a chave da manutenção; depois de desligado, as próximas chaves não reativam o alarme, inclusive após sair e retornar ao mapa.

Os puzzles da biblioteca permanecem como proposta: [cinco puzzles de leitura](CampusLibrary-Puzzles-Leitura.md). Os pickups ainda não estão bloqueados por esses enigmas.

Conferência desta rodada por arquivos e referências. Sem iniciar Unity, jogo, Play Mode, testes, build, capturas, commit ou push.
