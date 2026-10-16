# Library — revisão de abandono e quarentena

Recriação do interior de `Assets/_Project/Scenes/Library/Prefabs/Library.prefab` a partir da segunda imagem enviada pelo usuário. A composição anterior, com madeira ornamentada, tapetes e janela vermelha, foi substituída por uma biblioteca institucional arruinada. A decisão anterior de adaptar ao espaço e à altura existentes continua aplicada.

## Resultado

- Pilares de concreto chanfrados, reboco deteriorado, mofo, vigas aparentes e rodapés úmidos.
- Estantes com lacunas, uma fileira inclinada, móveis afastados e cadeiras derrubadas; livros, papéis, gesso e madeira acumulados em pontos de desabamento.
- Janelas com vidros quebrados, vegetação entrando e vista fotográfica de um pátio abandonado. Poças e dois cubemaps persistentes de reflexão.
- Fluorescentes danificados, luminárias apagadas, fios soltos e ventiladores de teto detalhados. Doze luzes locais, quatro com sombras, sem alteração de configurações globais.
- Quadro de quarentena, sinalização em português, porta de arquivo barricada e marcas localizadas de sangue seco. A porta cenográfica não substitui a entrada funcional da biblioteca.
- Materiais PBR, neblina e pós-processamento locais com os sistemas existentes `UniDoom/TotalChaosInspiredSurface` e `TCRegionAtmosphere`; nenhum novo gerador em runtime.

As instruções dos documentos foram utilizadas como orientação artística e técnica para este prefab. Não foi executada uma conversão global do projeto. ClassRoom, Basement e Cylinder permanecem como referências de integração ao projeto.

## Recursos e licença

Os modelos de mesas, cadeiras, estantes, arquivo e livros e suas texturas PNG são reutilizados da importação anterior da biblioteca. Novos downloads, autores, URLs e hashes estão em [asset-sources.json](asset-sources.json):

| Recurso | Origem | Licença |
| --- | --- | --- |
| Ventilador de teto, FBX e PNG 1K | [Ceiling Fan](https://polyhaven.com/a/ceiling_fan) | CC0 |
| Reboco deteriorado, PNG 2K | [Damaged Plaster](https://polyhaven.com/a/damaged_plaster) | CC0 |
| Concreto danificado, PNG 2K | [Concrete Floor Damaged 01](https://polyhaven.com/a/concrete_floor_damaged_01) | CC0 |
| Folhagem, PNG 1K com recorte alfa | [Tree Small 02](https://polyhaven.com/a/tree_small_02) | CC0 |
| Vista externa fotográfica, HDR 2K | [Abandoned Pathway](https://polyhaven.com/a/abandoned_pathway) | CC0 |

Licença oficial: [Poly Haven CC0](https://polyhaven.com/license). Albedos usam sRGB; normais e máscaras usam importação de dados. A conversão ARM e a composição do canal alfa da folhagem preservam os mapas baixados.

## Verificação e preservação

Unity 6000.5.7f1, URP 17.5.0. O projeto isolado `.utmp/LibraryPreview` é usado para compilar os geradores, salvar assets persistentes, abrir a SampleScene, renderizar capturas e executar Play Mode.

- [validation.txt](validation.txt): carregamento do prefab e da instância no mapa, scripts, materiais, meshes, referências e folgas para cápsula de jogador.
- [play-validation.txt](play-validation.txt): CharacterController com dimensões do Player existente, movimento com colisão e gravidade nas rotas de entrada e corredores central/lateral, além de seleção da atmosfera regional.
- [installed-validation.json](installed-validation.json): hashes anteriores/posteriores e colisões de novos GUIDs. O instalador altera somente o prefab entre os arquivos já existentes de Assets, Packages e ProjectSettings.
- [structure-validation.json](structure-validation.json): preservação dos 44 IDs originais do casco e exame de consumidores externos antes de substituir a decoração anterior.
- [installed.json](installed.json): arquivos instalados e hashes. As novas dependências estão em subpastas `DecayRevision` dentro da Library.

Não foram realizados build standalone, combate completo, bake de lightmaps ou medição de FPS no hardware alvo. As capturas são renderizações da Unity sem lanterna; não são imagens geradas para simular o resultado.

O [log final](unity-final.log) também registra uma `ArgumentOutOfRangeException` no indexador `UnityEditor.Search` ao entrar em Play Mode. A mesma exceção já aparecia no log da validação anterior, antes desta revisão. Os testes de carregamento, referências e circulação terminaram com os marcadores `LIBRARY_DECAY_VERIFY_PASS` e `LIBRARY_DECAY_PLAY_PASS`; isso não significa que o Console completo do Editor esteja sem erros.

A SampleScene foi salva no Editor durante esta tarefa, depois do inventário inicial. Essa alteração simultânea foi mantida, registrada em [concurrent-scene.json](concurrent-scene.json) e copiada para `ConcurrentMainScene.unity` como evidência fora de Assets. A versão mais recente foi sincronizada para o projeto isolado e conferida novamente na Unity; veja [unity-latest-scene.log](unity-latest-scene.log). O teste físico em Play Mode usa o mesmo prefab final; as capturas no mapa correspondem à cena mais recente. Nenhuma restauração da cena inicial foi aplicada ao projeto principal.

Prévia no mapa: [quarentena](InMap-Quarantine.png), [entrada](InMap-Entrance.png), [mesas e janelas](InMap-WindowTables.png).

## Regeneração e reversão

`Before/Library` preserva todos os assets da biblioteca imediatamente anteriores a esta revisão, incluindo metas; `before-manifest.json` registra o estado anterior do projeto. A primeira versão e suas evidências continuam em `Tools/Library`.

As ferramentas desta pasta são pontuais e ficam fora de Assets. Para regenerar, use uma cópia isolada do projeto e copie `LibraryDecay*.cs` para `Assets/_TemporaryTools/Editor`, junto com `LibraryBuilder.cs` da geração anterior. Execute `LibraryDecayBuilder.Build`, `python Tools/Library/DecayRevision/install.py`, e então `LibraryDecayVerifier.VerifyThenPlay` na Unity isolada, sem `-quit`. Apenas depois dos marcadores PASS execute `install.py --install` e `verify_installed.py` no projeto principal. Não execute geradores históricos sem adaptar os caminhos segundo `Tools/ProjectOrganization/path-map.json`.

Para reverter a aparência, restaure o prefab e os assets correspondentes de `Before/Library` com suas metas após verificar alterações posteriores. Os recursos anteriores foram mantidos no projeto; nenhum asset foi apagado permanentemente nesta revisão.
