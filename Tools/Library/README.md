> Versão atual: [revisão de abandono e quarentena](DecayRevision/README.md). O restante deste documento registra a primeira composição, substituída pela nova referência.

# Library — biblioteca abandonada

Prefab: `Assets/_Project/Scenes/Library/Prefabs/Library.prefab`.

A composição usa a imagem fornecida: estantes altas, mesas de leitura, luminárias verdes de latão, tapetes ornamentados, arquivo, computador antigo, busto e janela vermelha. O acabamento segue a skill Total Chaos enviada, idêntica à instalada, e a linguagem de ClassRoom, Basement e Cylinder. Conforme a escolha do usuário, o espaço e a entrada existentes foram mantidos, sem mezanino.

## Implementação

- Unity 6000.5.7f1 e URP 17.5.0; reutilização de `UniDoom/TotalChaosInspiredSurface` e `TCRegionAtmosphere`.
- Sete modelos FBX CC0 com materiais PNG PBR: estante, enciclopédias, cadeira, mesa, busto, escada e arquivo. Um volume individual do conjunto serve às pilhas nas mesas.
- Prateleiras preenchidas a partir dos planos de apoio identificados na geometria importada. Colisores simples; móveis em escala métrica na instância do mapa.
- LODs persistentes para os modelos repetidos; malhas e materiais compartilhados entre instâncias. As malhas originais baixadas continuam disponíveis para regeneração.
- Trinta luzes locais; cinco projetam sombras. Piso/teto divididos em células para respeitar as quatro luzes adicionais por objeto da configuração URP existente. Não foram alteradas configurações globais de qualidade.
- Volume local com ACES, dessaturação moderada, bloom, vinheta e grain sutis. Fontes apagadas e sujeira concentrada em rodapés, cantos e janelas.
- A decoração é salva no prefab. Nenhum gerador novo roda em runtime. O prefab mantém GUID e os IDs originais; as alterações permitidas nos blocos antigos são registradas em `structure-validation.json`.
- A escala raiz passa a corresponder à escala que a instância da SampleScene já usava. A decoração compensa essa escala para preservar as proporções dos móveis. Os overrides e os bytes da SampleScene são preservados.

## Procedência

Os arquivos e hashes de cada download estão em [asset-sources.json](asset-sources.json), [rug-source.json](rug-source.json) e `Manifests/`. Os mapas ARM são convertidos para o layout metallic/AO/roughness/smoothness usado pelo shader; albedo usa sRGB, normal e máscaras usam dados lineares, com mipmaps e compressão.

| Recurso | Origem | Licença |
|---|---|---|
| Estante | [Wooden Bookshelf Worn](https://polyhaven.com/a/wooden_bookshelf_worn) | CC0 |
| Livros | [Book Encyclopedia Set 01](https://polyhaven.com/a/book_encyclopedia_set_01) | CC0 |
| Cadeira | [Dining Chair 02](https://polyhaven.com/a/dining_chair_02) | CC0 |
| Mesa | [Wooden Table 02](https://polyhaven.com/a/wooden_table_02) | CC0 |
| Busto | [Marble Bust 01](https://polyhaven.com/a/marble_bust_01) | CC0 |
| Escada | [Wooden Ladder](https://polyhaven.com/a/wooden_ladder) | CC0 |
| Arquivo | [Drawer Cabinet](https://polyhaven.com/a/drawer_cabinet) | CC0 |
| Tecido e reboco | [Dirty Carpet](https://polyhaven.com/a/dirty_carpet), [Worn Mossy Plasterwall](https://polyhaven.com/a/worn_mossy_plasterwall) | CC0 |
| Tapete ornamental | [The Met, ‘Transylvanian’ Carpet, séc. XVII](https://www.metmuseum.org/art/collection/search/452565) | CC0, imagem convertida para PNG |
| Madeira, piso e metal reutilizados | `ClassRoom/Surface/Textures`: wood_table_worn, worn_tile_floor, rusty_metal_02; procedência já registrada no projeto | CC0 / Poly Haven |

Políticas verificadas: [Poly Haven](https://polyhaven.com/license) e [The Met Open Access](https://www.metmuseum.org/policies/image-resources). As imagens de Total Chaos são referências de direção visual; seus assets não foram extraídos nem copiados.

## Validação e recuperação

- `Before/Library.prefab` e seu meta preservam a versão recebida. `before-manifest.json` registra hashes dos arquivos anteriores em Assets, Packages e ProjectSettings.
- `LibraryAudit.cs`, `LibraryBuilder.cs`, `LibraryFinish.cs`, `LibraryVerifier.cs` e `LibraryPlayCheck.cs` são fontes de ferramentas pontuais. São compiladas somente na cópia `.utmp/LibraryPreview/Assets/_TemporaryTools/Editor`, fora dos Assets do projeto principal.
- `install.py` primeiro aplica apenas os campos autorizados aos blocos YAML originais, preserva todos os fileIDs e exige nova validação Unity antes da instalação. Não instala malhas/máscaras geradas que não sejam dependências do resultado ou dos módulos reutilizáveis.
- Capturas `Library-*.png` mostram o prefab; `InMap-*.png` mostram a instância real na SampleScene. PNGs usam render target sRGB para evitar capturar valores lineares como se fossem cores de exibição.
- Resultados executados ficam em `validation.txt`, `play-validation.txt`, `structure-validation.json` e `installed.json`. `counts.txt` inclui todas as alternativas de LOD simultaneamente; essa soma não representa triângulos renderizados por frame.
- Não foram feitos bake de iluminação, build standalone, teste completo de combate/controles nem benchmark de FPS em hardware-alvo.
- Durante a entrada em Play Mode, o indexador `UnityEditor.Search.SearchDatabase` lançou uma `ArgumentOutOfRangeException` na cópia isolada. A travessia e a seleção/restauração da atmosfera concluíram depois disso (`LIBRARY_PLAY_PASS`). Esse registro do Editor não é atribuído aos scripts do cenário nem apresentado como uma Console sem erros.

Revisão da skill: escala, distinção dos materiais, fontes de luz, navegação, profundidade, uso anterior, contato dos props, legibilidade, identidade e pós-processamento receberam 2/2 na revisão das capturas e verificações disponíveis. Desgaste causal recebeu 1/2: há sujeira localizada, mas a coleção mantém repetição visual intencional. Desempenho não recebeu nota sem benchmark no hardware-alvo; não se declara aprovação global desse item por inferência.

Para reversão seletiva, compare o prefab atual com `installed.json` antes de restaurar o backup. Preserve o meta original. As novas dependências podem permanecer sem uso; não as apague sem auditar consumidores. Não use a reversão geral da reorganização anterior para desfazer esta revisão de arte.
