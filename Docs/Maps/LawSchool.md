# Bloco de Direito — LawSchool

Atualizado: **2026-11-05**. Estado: ampliação, revisão visual e testes concluídos. Esta ficha é contexto de manutenção; não é gerador nem substitui inspecionar a versão atual dos assets.

## Leitura rápida

- Unity **6000.5.7f1**, URP **17.5.0**, Windows/D3D12. Confirme versões nos arquivos do projeto somente se mudaram.
- Interior térreo com hall central, quatro corredores, **32 salas de aula**, oito banheiros, administração/apoio e auditório.
- Ampliação horizontal **2×**, área aproximadamente **4×** a versão inicial. Player e mobiliário preservam escala real.
- Salas: **10,8 m × 9,4–13,6 m**, 24–42 cadeiras; seminários e audiências têm arranjos próprios.
- Banheiros: **7,6 m × 9,4–13,6 m**, quatro cabines e três cubas; mictórios nos masculinos. Auditório: **112 lugares**.
- Entrada pelo prédio de Direito no Campus Exterior e pelo menu temporário. Não crie outra cena ou outra opção de menu para o mesmo mapa.

## Pedido do usuário e decisões aceitas

Criação: usar `.codex/reference/direito reference.png`, ligar o interior ao prédio do campus Unimar, incluir no menu temporário, aplicar a skill Total Chaos e o prompt mestre, otimizar e usar modelos 3D gratuitos compatíveis.

Variação: cada sala deve ter uma peculiaridade — lotação, carteiras, lousa lateral/oposta à porta, equipamentos, material de estudo ou situação de abandono.

Correção posterior, que **prevalece sobre o layout compacto anterior**:

> Faça o mapa bem maior [...] as salas devem parecer igual as de faculdade, o mesmo vale para os banheiros [...] levando em consideração o tamanho do player [...] o bloco precisa parecer um bloco de direito de uma faculdade [...] estilo mais assustador [...] semelhante a total chaos.

O usuário autorizou ampliar o interior. O traçado externo do campus permanece preservado. Não volte às salas iniciais de 5,4 m de largura e 4–16 cadeiras. Salas administrativas podem ter poucas mesas; diferencie-as de salas de aula pelo uso e pela sinalização.

## Arquivos e identidade

Prefixo exclusivo: `Assets/_Project/Scenes/LawSchool/`.

| Conteúdo | Caminho relativo ao projeto |
|---|---|
| Cena jogável | `Assets/_Project/Scenes/LawSchool/LawSchool.unity` |
| Prefab principal | `Assets/_Project/Scenes/LawSchool/Prefabs/LawSchool.prefab` |
| Materiais e meshes persistentes | `Assets/_Project/Scenes/LawSchool/Materials/` e `Meshes/` |
| Mobiliário escolar | `Prefabs/SchoolChair_01.prefab`, `Prefabs/SchoolDesk_01.prefab` sob o prefixo |
| Sanitários | `Models/Sanitary/`, `Textures/Sanitary/`, `Prefabs/Sanitary_*.prefab` sob o prefixo |
| Iluminação indireta necessária | `Assets/_Project/Scenes/LawSchool/Scripts/LawSchoolLighting.cs` |
| Pós-processamento | `Assets/_Project/Scenes/LawSchool/Profiles/LawSchool.asset` |
| Oclusão nativa | `Assets/_Project/Scenes/LawSchool/LawSchool/OcclusionCullingData.asset` |
| Referência enviada pelo usuário | `.codex/reference/direito reference.png` |
| Menu compartilhado | `Assets/_Project/Global/Scripts/UI/GameMenus.cs` |
| Sessão e criação do player | `Assets/_Project/Global/Scripts/Core/GameManager.cs`; raiz `SpawnPoint` da cena |
| Cenas habilitadas | `ProjectSettings/EditorBuildSettings.asset`; conferir também o Build Profile ativo |
| Fontes, licenças, hashes | `Licenses/AssetSources/LawSchool/sources.json`, `sanitary-sources.json`, `sanitary-readme.txt` |

GUIDs úteis: cena `4c02879dcd5ae9c41bbb62dae57b8451`; prefab principal `8f2dc05494d3c2b43ac0531067ae8583`; oclusão `58b8f20070a67144091fb120fea2ba75`; cadeira `54c99ad2d5a2e8f429d60d024844acbe`; carteira `2f9f62908021eda41b3812a84cea2326`.

A pasta aninhada `LawSchool/LawSchool/` é usada pela Unity para a oclusão; não reorganize só pelo nome repetido. Não renumere fileIDs. Alterações da cena e do prefab devem permanecer coerentes.

`Meshes/Part_*`, `Finish_*`, `FixtureReview_*`, `University_*` e `Atmosphere_*` são resultados persistentes referenciados pelo mapa. O prefixo de geração não os torna descartáveis; verifique dependências antes de qualquer limpeza.

## Layout, escala e circulação

- Raiz: `Bloco de Direito — pavimento único`; salas `SALA 101` a `SALA 132`. Cada ala contém salas e serviços.
- Hall no eixo X=0; centros dos corredores em Z=17,2 e Z=43,2, nos dois lados. Auditório após Z=60. Interior ocupa aproximadamente 135 m × 80 m.
- Altura livre aproximada: 3,25 m. Vãos de salas: aproximadamente 1,4 m, com batentes. Preserve o piso, as junções e a abertura efetiva da folha.
- Player: `Assets/_Project/Global/Prefabs/Player/Player.prefab`; escala da raiz `(0,4733; 2,1210; 0,4197)`. A cápsula resulta em **2,121 m de altura e 0,473 m de diâmetro**. Meça o resultado em mundo, não só `CharacterController.height` local. **Não redimensione o player para fazer a sala caber.**
- Cadeira 0,88 m; carteira 0,76 m de altura. Amplie arquitetura e reorganize o mobiliário sem esticar cadeiras, vasos ou hitboxes. Confira contato com piso/tampo e o espaço de uso.
- Fileiras têm passagem central e laterais; lousas laterais exigem orientação correspondente das carteiras. Seminários circulares e audiências simuladas são variações intencionais.
- Portas existentes usam `OpenDoorWithKey`, sem chaves e sem reload de shaders. Algumas salas possuem vãos abertos de manutenção: o transform `Porta da sala ...` existe mesmo sem esse componente.

## Conexões

| Origem | Destino/ponto | Posição em mundo |
|---|---|---|
| Prédio `04A Law school exterior` no campus | `LawSchool` / `FromCampus` | Entrada externa aprox. `(-43; 1,85; 14,4)` |
| Chegada no interior | `FromCampus` | `(0; 1,12; 4,20)`; olhando para o hall |
| Saída do hall | `CampusExterior` / `FromLawSchool` | Trigger perto de `(0; 1,25; 0,5)` |
| Chegada de volta ao campus | `FromLawSchool` | `(-43; 1,75; 12,40)`; yaw 180° |

Portais: `Assets/_Project/Global/Scripts/Utils/SceneTransitionDoor.cs`; chegadas: `SceneArrivalPoint.cs` na mesma pasta. Cena e prefab do campus ficam em `Assets/_Project/Scenes/CampusExterior/`. GUID da cena do campus: `faaefa97714810b429c19b5065aff68d`.

Menu: botão `TemporaryMapSelect` → `Map_LawSchool`; rótulo **BLOCO DE DIREITO**. A lista deriva das cenas habilitadas. Ao verificar TMP de botão, procure o texto nos filhos; o primeiro TMP pode ser a seta de seleção.

O `spawnPoint` serializado do `GameManager` aponta para a raiz `SpawnPoint` deste interior. Preserve serviços, câmera e HUD compartilhados; não copie outro manager mantendo o spawn de outro mapa.

## Direção visual e assets

Norma: `.codex/SKILL.md`, seções Geometria e escala, Texturas e materiais, Iluminação, Composição e narrativa e Checklist. Referência: **mod clássico Total Chaos no ModDB**, não presumir técnicas do remake nem copiar seus assets.

Madeira envelhecida, reboco frio, piso gasto, sinalização azul e fluorescentes definem a universidade. Desgaste concentra-se em infiltrações, rodapés e zonas de uso; há forro modular, luzes mortas, documentos e alguns móveis deslocados. Preserve leitura de portas, obstáculos e sinalização; não resolva o horror enterrando a cena em preto.

Autores/fontes principais: **Ethan Place / Poly Haven**, cadeiras e carteiras; **loafbrr_1 / OpenGameArt**, vasos, cubas e mictórios. Links e licenças estão em [Referencias-e-Assets.md](Referencias-e-Assets.md). Texturas compartilhadas reaproveitadas: `Assets/_Project/Global/Surface/ClassRoom/Textures/` e reboco em `Assets/_Project/Global/Textures/Library/DecayRevision/damaged_plaster/`. Planta reaproveitada de `Assets/_Project/Scenes/EntranceHallway/Prefabs/CampusReference/CampusPottedPlant.prefab`.

PBR: texturas sanitárias e escolares em 1K; normal maps importados como normal, máscaras em espaço linear. ARM/AoRM destes assets foram convertidos para Unity Lit: **R=metallic, G=occlusion, A=1−roughness**. Os manifestos conservam hash de origem/conversão. Não converta novamente uma máscara já convertida.

Iluminação: `LawSchoolLighting` mantém ambiente/fog consistentes ao trocar de cena com câmera persistente. Luzes locais sem sombras na maioria; fontes selecionadas usam sombra. No PC, o tier **Medium do UniversalAdditionalLightData é 512 px**; o atlas é 2048. Em URP, alterar só `Light.shadowResolution` não substitui conferir o tier adicional. Não altere configurações globais para resolver apenas este mapa.

## Validação já executada

Em 2026-11-05: build Windows Development de **LawSchool + CampusExterior**, zero erros; teste real do menu, 32 entradas de salas, oito banheiros, corredores internos, portais nos dois sentidos e passagem pela porta/eixo central/palco do auditório. Auditoria estática: **2.406 amostras**, sem falta de piso nem obstruções para a cápsula real. Oclusão recalculada e persistente.

RX 6400, D3D12, janela 1600×900, cinco vistas fixas, mediana de 180 amostras após aquecimento:

| Vista | Frame ms | GPU ms |
|---|---:|---:|
| Hall | 4,612 | 4,101 |
| Corredor | 6,163 | 5,553 |
| Sala | 5,543 | 5,003 |
| Banheiro | 4,289 | 3,851 |
| Auditório | 4,313 | 3,897 |

Valores absolutos de uma máquina; não são promessa de FPS geral nem comparação equivalente com a planta pequena. Não testados: build das nove cenas, automação de Play Mode do Editor, tela cheia, outras GPUs, VRAM/GC/overdraw e certificação de acessibilidade.

Identificação SHA256 da versão validada (compare sem ler o YAML inteiro):

| Arquivo sob o prefixo do mapa | SHA256 |
|---|---|
| `LawSchool.unity` | `3844708f84b3c4a30ed561f5782a1972108c18673d3be25da0db0e7dbc931262` |
| `Prefabs/LawSchool.prefab` | `2bbf6c7c9f6cce29f6b5cb2ff274a758ae622e208a8dd4968dc3ef4c6d6a9eb8` |
| `LawSchool/OcclusionCullingData.asset` | `b23c5ab5b5c5ea00d3a13c166c1d1fe79846f65e94274afc9ebdafe3e716c66f` |

Esses hashes identificam somente os três arquivos; materiais, importadores, código e demais dependências também podem invalidar um teste anterior.

Capturas duráveis: `output/Direito-Hall.png`, `Direito-Salas-Ampliadas.png`, `Direito-Banheiros-Ampliados.png`, `Direito-Auditorio.png`. Evidências detalhadas, quando ainda presentes: `%TEMP%/UniDoomLawExpansion/{validation.txt,build.txt,occlusion.txt,cleanup.txt,runtime/,final-runtime/,review/}`. O mapa **não depende** desses temporários. Ferramentas desta ampliação foram removidas após verificar dependências; as oito ferramentas Editor preexistentes foram preservadas.

## Manutenção sem repetir investigação

1. Leia o pedido novo e esta ficha; veja a referência e uma ou duas capturas relevantes. Localize os objetos pelo Editor, sem despejar o YAML inteiro da cena/prefab.
2. Inspecione estado real e alterações existentes. Não rode geração sobre Play Mode. Reconcilie alterações não salvas antes de reabrir uma cena.
3. Ajuste somente a categoria afetada. Salve resultados persistentes por Unity API. Refaça oclusão se mudar paredes/vãos/occluders; mudanças apenas em docs não pedem novo build.
4. Valide o delta: escala/colisões → cápsula e percurso; iluminação → capturas/frame time; portais/menu → transições/build list. Expanda testes se houver falha ou dependência alterada.
5. Atualize esta ficha, os manifestos de fontes e o que foi efetivamente testado. Não reutilize automaticamente estes resultados se cena ou dependências mudaram.

Armadilhas observadas:

- `Renderer.bounds` de um FBX carregado só como asset pode retornar zero. Use `Mesh.bounds`/vértices transformados ou uma instância de preview para normalizar dimensões.
- Copiar um `Mesh` com `CopySerialized` pode deixar buffers/limites desatualizados. Se ocorrer, reaplique dados via `Mesh.Clear/SetVertices/SetTriangles`, recalcule bounds/normais/tangentes conforme necessário, reassocie o mesh e confirme salvo/reaberto e renderizado. Preserve a topologia; não presuma organização de vértices sem inspecionar.
- `VolumeProfile` criado por código precisa dos componentes adicionados ao asset para persistirem. Referências runtime necessárias devem existir antes de retirar o gerador.
- Use `EditorSceneManager.NewPreviewScene()` para auditoria isolada; não confunda vãos abertos com portas faltantes. Depois da cópia, consulte transforms de entrada, não exija componente de porta em toda sala.
- Editor aberto no mesmo projeto: não inicie outro Editor em batch. Um `MenuItem` temporário explícito é mais confiável que depender somente de polling em Play Mode. No Windows, o menu nativo `Assets/Refresh` pode ser acionado por `GetMenu/GetSubMenu/GetMenuItemID` e `WM_COMMAND`, sem enviar Ctrl+R para outro aplicativo. Resolva IDs dinamicamente; nunca fixe PID/handle.
- Teste de FPS exige janela realmente renderizando, triângulos não zero e aquecimento. Janela oculta/minimizada não fornece benchmark comparável.
- Confira espaço de disco. O primeiro build em `%TEMP%` falhou por C: cheio; o build válido usou `Temp/LawSchoolExpansionBuild` no disco do projeto. Temporários do projeto podem desaparecer ao fechar a Unity; mantenha documentação permanente fora deles.

## Solicitações originais — consulta opcional

Trechos integrais dos três pedidos que definem este mapa, preservando a redação do usuário. O resumo operacional está no início; não é necessário reler este apêndice em toda edição.

Criação:

> Seguinte, tem dentro da pasta de refenrece , um png chamado direito reference, usa ele de referência, e cria esse mapa, já faz ele ligado ao prédio no campus entrance da unimar, e já adiciona ele na tela do menu principal naquele menu temporário, usa as skills e o prompt mestre passado, faça o mapa ser otmizado, utilize modelos 3d gratuitos sempre que possível

Variação das salas:

> tenta faze as salas um pouco diferente umas das outras, tipo, uma sala de aula ter menos cadeiras que a outra, a lousa tá do lado oposto da porta, etc, faz cada ter uma peculiaridade

Ampliação e escala, pedido mais recente:

> Faça o mapa bem maior, há salas muito pequenas comm1 carteira, as salsa devem parecer igual as de faculdade, o mesmo vale para os banheiros, faça o mapa levando em consideração o tamanho do player, não se preocupe do mapa ficar grande, o bloco precisa parecer um bloco de direito de uma faculdade, o espaço precisa ter um estilo mais assustador se possível semelhante a total chaos, reanalise o mapa, faça as alterações necessárias
