# Library — mesas, iluminação e porta simples

Alteração do `Assets/_Project/Scenes/Library/Prefabs/Library.prefab` a pedido do usuário:

- Onze mesas de leitura com tampos 30% maiores em largura e profundidade e altura ajustada de aproximadamente 79 para 82 cm. Cadeiras afastadas 23% do centro e objetos elevados para continuar apoiados nos tampos. Colisores acompanham as mesas.
- Ambiente e pós-processamento locais baseados nos valores reais da sala de aula: ambiente RGB 0,09 / 0,105 / 0,12, fluorescentes com intensidade 2 e alcance 9 m, luz das janelas com intensidade 0,75 e alcance 7 m e perfil derivado de `ClassRoomVisibility`. As três luzes defeituosas continuam presentes. A alteração fica na biblioteca; configurações globais e outros ambientes não são modificados.
- Porta simples de uma folha na entrada da biblioteca, com batente, acabamento pintado e maçaneta. Reutiliza `OpenDoorWithKey` com todos os requisitos de chave desmarcados; abre por aproximação e fecha após a passagem.
- A porta barricada lateral fica desativada por inteiro, incluindo sua colisão e lâmpada. A parede existente fica visível no lugar. O objeto foi preservado inativo para reversão e manutenção dos fileIDs.

As novas dependências ficam em subpastas `LayoutRevision` de Materials, Profiles, Reflections, Meshes e Textures dentro da Library. Reutilizam texturas e scripts existentes. Dois novos cubemaps acompanham a iluminação mais baixa; os anteriores são preservados. Não há gerador novo executando em runtime.

O acabamento externo da entrada foi completado e ganhou uma placa legível dos dois lados. Os textos estáticos da biblioteca foram renderizados com a fonte existente para PNGs recortados e aplicados em quads que respeitam a profundidade: deixam de aparecer através da porta e das paredes. Os TextMeshes e seus fileIDs continuam presentes com os renderers antigos desativados; para mudar suas frases depois, atualize também os PNGs com `LibraryDepthSigns`. Não foram sintetizadas texturas complexas para substituir mapas reais.

Um objeto `Right Wall` próximo à entrada usava o material verde de protótipo `DoorColor`. A cópia isolada confirmou essa origem e sua troca por reboco; a correção do material é instalada na SampleScene. Nenhuma propriedade de chave, mecanismo ou geometria de outra porta foi alterada.

## Validação

Unity 6000.5.7f1 / URP 17.5.0 em cópia isolada do projeto. A ferramenta verifica meshes, shaders, materiais, scripts e referências do prefab e sua instância na SampleScene; captura [a biblioteca no mapa](InMap-Quarantine.png) e [a porta](InMap-Door.png). No exame estático dos corredores, a folha fechada é desconsiderada; a abertura e a passagem são verificadas separadamente em Play Mode.

O teste físico usa as dimensões do CharacterController do Player existente, gravidade e colisão. Aproxima-se da porta com `PlayerPickupKey` sem chaves, espera sua abertura, atravessa a entrada e percorre os corredores central e lateral. Confere abertura além de 70 graus e fechamento após sair do gatilho. Uma plataforma externa aparece apenas na cena temporária desse teste para permitir a aproximação ao prefab isolado; não é instalada no mapa.

Evidências: [validação](validation.txt), [Play Mode](play-validation.txt), [preservação e instalação](installed-validation.json) e [log Unity](unity-validation.log). A exceção preexistente do indexador `UnityEditor.Search` pode aparecer ao iniciar Play Mode; os marcadores dos testes são conferidos separadamente. Não foram executados build standalone, benchmark de FPS ou teste completo de combate.

## Preservação

O prefab anterior e sua meta, a SampleScene e sua meta e uma cópia de leitura do Cylinder estão nesta pasta como registro anterior. `before-manifest.json` contém hashes de Assets, Packages e ProjectSettings. O instalador mantém todos os fileIDs existentes do prefab, verifica os transforms originais do casco, novos GUIDs e hashes. Entre os arquivos já existentes do projeto, somente o prefab Library e o material do trecho da entrada na SampleScene são alterados. Na cena, transplanta exclusivamente os campos de materiais validados na Unity, mantendo os blocos originais, meshes, scripts e transforms; [scene-material-overrides.json](scene-material-overrides.json) registra os campos. Alterações simultâneas do Editor nesses arquivos impedem a sobrescrita. Os recursos anteriores permanecem disponíveis.

As ferramentas são pontuais e ficam fora de Assets. Para repetir a geração, sincronize os assets atuais para `.utmp/LibraryPreview`, copie `LibraryLayoutRevision.cs`, `LibraryDepthSigns.cs`, `LayoutVerifier.cs` e `LayoutPlayCheck.cs` para `Assets/_TemporaryTools/Editor` da cópia, mantendo `LibraryBuilder.cs` da primeira geração, e execute `LibraryLayoutRevision.BuildAndVerify` sem `-quit`, seguido de `LibraryLayoutRevision.FixSceneAndVerify`. A geração foi preparada para aplicar a transformação uma vez sobre o prefab de `CampusFinish`. Execute `install.py` somente após os três marcadores PASS. Não execute o gerador de novo sobre o resultado sem restaurar a cópia anterior ou adaptar as transformações.
