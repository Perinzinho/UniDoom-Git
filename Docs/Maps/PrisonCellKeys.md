# Chaves numeradas das celas

Atualizado em 08/11/2026. Prefabs persistentes de LawSchool, CampusLibrary, CampusBlock4 e Basement; cenas e portais existentes preservados.

| Chave/cela | Professor | Local da chave |
|---|---|---|
| 1 | Caio | Direito: chave original sorteada entre 33 ambientes, com alarme original |
| 2 | Fabio | Direito: mesa docente da sala 101 |
| 3 | Henrique | Direito: mesa docente da sala 102 |
| 4 | Santi | Biblioteca: mesa de estudo individual |
| 5 | Matheus Ferraroni | Biblioteca: mesa de leitura em carvalho |
| 6 | Navarro | Biblioteca: bancada de pesquisa |
| 7 | Paulinho | Bloco 4: chave original do puzzle na bancada da 423 |
| 8 | Rafa | Bloco 4: mesa docente da 428 |

Inventário existente `PlayerCellKeys`, IDs `professor-cell-01` até `professor-cell-08`. `PrisonCellGate` exige exclusivamente sua chave. `PrisonProgress` mantém o estado de portas abertas e chefe; não há chave universal ativa no porão. O pickup universal antigo permanece salvo e inativo, com fallback restrito à cela 1. `CellKeyPickup` trata as seis novas chaves; os pickups originais do alarme/puzzle continuam responsáveis pelas chaves 1/7. Coleta e retorno ao mapa consultam o inventário persistente da sessão. A HUD do porão mostra as chaves possuídas e o número exigido por cada cela. Não foi criado outro sistema de save.

Sprites: atlas compartilhado `Assets/_Project/Global/Resources/Textures/Pickups/CellKeys.png`, oito subassets `CellKey01`…`CellKey08`; renderizadores usam o material de sprite URP e Billboard existentes. Fonte, prompt e recortes em `Licenses/AssetSources/CellKeys/generated-sprites.json`.

Onze gravações existentes tratadas, mantendo caminhos, GUIDs, importadores e associações. Ruído moderado reduzido, silêncios retirados, volume medido entre −18,30 e −17,98 LUFS e pico verdadeiro máximo −1,50 dBTP. Cortes confirmados pelo usuário: Henrique sem “calma”, mantendo “Vai Corinthians”; Bianca somente “yipeee” final. Medições e hashes em `Licenses/AssetSources/Teachers/audio-edit.json`. Os professores Ferraroni, Navarro e Rafa já não tinham áudio associado; nenhum áudio de outro professor foi atribuído a eles. Todos os onze arquivos fornecidos foram processados, inclusive os ainda sem consumidor.

Validação: matriz 8 × 8 de identidade, duplicatas, IDs inválidos e sprites carregados em objetos isolados no modo de edição; prefabs reabertos, referências e IDs preservados; previews fora de tela em `output/Campus-Ultimas-Chave-*.png`; hashes e métricas dos áudios conferidos. Sem executar jogo, Play Mode, ativar/focar janela ou testar coleta, retorno entre mapas, áudio/HUD em runtime, build ou FPS.
