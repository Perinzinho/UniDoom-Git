# Superfícies do ClassRoom — Total Chaos clássico

Unity 6000.5.7f1 / URP 17.5. Continuação da [iluminação validada](../Lighting/README.md),
usando a referência enviada pelo usuário para paleta fria, desgaste e contraste.
Aplicada às paredes, teto, piso, madeira, porta e metal do ClassRoom.

## Implementação

- `Assets/TotalChaos/Shaders/TotalChaosInspiredSurface.shader`: deriva os passes
  nativos do URP Lit instalado. A extensão HLSL acrescenta sujeira e umidade nas
  cavidades, saturação e contraste de superfície, preservando PBR, normal/detail
  mapping, AO, iluminação, sombras, reflexos, fog, instancing e SRP Batcher.
- Os controles usam o vetor de especular, reservado e oculto neste shader de
  workflow metálico, mantendo o layout nativo do `UnityPerMaterial` em todos os
  passes. O inspector expõe sliders separados. Specular workflow não é suportado;
  materiais desse workflow são excluídos da conversão.
- A máscara Unity empacota **R = metallic, G = AO, B = roughness,
  A = 1 − roughness**. É conversão de canais do ARM baixado; nenhuma textura
  complexa foi sintetizada. O dirt mask lê cavidades no canal verde.
- Normais GL e máscaras são lineares; albedo é sRGB. Mipmaps, filtragem trilinear,
  anisotropia 4 e compressão de alta qualidade. Concreto em 2K; os demais em 1K.
  Parallax fica desligado por padrão e pode ser habilitado no inspector.
- Os materiais antigos permanecem intactos. Foram criadas 12 versões novas,
  associadas por GUID aos originais; 368 renderizadores recebem essas referências.
  Vidro, sangue, tecido dos corpos, textos, emissivos, UI e efeitos especiais
  conservam seus materiais. A reflexão estática foi recapturada em um asset novo.
- Luzes, exposição, pós-processamento, transições e orçamento de sombras da
  revisão anterior permanecem preservados. A madeira recebe calibração de albedo
  para manter leitura sem aumentar as luzes; o shader limita a refletância a 0–1.

## Texturas e licenças

Powered by [Poly Haven](https://polyhaven.com). As texturas são **CC0-1.0**, conforme
a [licença oficial](https://polyhaven.com/license). As fontes, URLs de download,
autores, resolução, tamanho e hashes SHA-256 estão em
`Assets/Art/ClassRoom/Surface/texture-sources.json`.

| Conjunto | Aplicação |
|---|---|
| [Concrete Wall 006](https://polyhaven.com/a/concrete_wall_006) | Alvenaria, teto deteriorado e entulho |
| [Rusty Metal 02](https://polyhaven.com/a/rusty_metal_02) | Metal pintado oxidado |
| [Wood Table Worn](https://polyhaven.com/a/wood_table_worn) | Mobiliário e porta |
| [Worn Tile Floor](https://polyhaven.com/a/worn_tile_floor) | Piso cerâmico envelhecido |
| [Rock Face](https://polyhaven.com/a/rock_face) | Preset reutilizável de pedra; não aplicado à arquitetura da sala |

Cada conjunto inclui PNGs de albedo, normal GL, ARM e height. ARM reúne AO,
roughness e metallic. Os PNGs originais baixados são preservados sem alterações.
O shader derivado segue a **Unity Companion License**, preservada em
`Assets/TotalChaos/Shaders/LICENSE-Unity.md`; a extensão própria usa os includes da URP.

## Conversão e reversão

Menu **Tools → Total Chaos → Material Converter**:

1. Selecione o objeto de hierarquia e os materiais desejados; o botão de seleção
   lista apenas materiais elegíveis. Escolha Concrete, Metal, Wood, Stone,
   Deteriorated ou Tiles.
2. **Convert selected materials** cria cópias e altera as referências da hierarquia.
   Os assets originais não são sobrescritos. Undo é suportado.
3. **Revert converted references** restaura materiais e a reflexão original da sala.
   Salve normalmente a cena ou os overrides do prefab, conforme seu fluxo no editor.

Para reconstruir: sincronize uma cópia isolada `.utmp/BasementPreview`, copie
`ClassRoomSurfaceBuilder.cs` e os verificadores anteriores para `Assets/Editor`.
`download_textures.py` obtém arquivos pelas URLs dos manifests oficiais e verifica
suas assinaturas PNG e hashes quando fornecidos. `build_shader.py` deriva o shader
da versão instalada de URP, sem modificar o pacote.

Use `ClassRoomSurfaceBuilder.Build`; preserve antes os dois prefabs e um manifesto
SHA-256 de Assets/Packages/ProjectSettings em `.utmp/ClassRoom-PBR/before`.
`install_surface.py` prepara somente listas de materiais e a referência do cubemap,
preservando todos os IDs e demais blocos YAML. Execute `ClassRoomSurfaceBuilder.VerifyAll`
com `-batchmode -quit` e log `.utmp/ClassRoom-PBR-final.log`; execute
`ClassRoomLightingVerifier.RunPlayMode` sem `-quit`, log `.utmp/ClassRoom-PBR-play.log`.
Depois, `install_surface.py --install` exige os resultados e recusa alterações concorrentes.

Capturas: [antes](PBRBefore-Aisle.png), [depois](PBRFinal-Aisle.png),
[frente](PBRFinal-Front.png), [porta](PBRFinal-Door.png),
[lanterna](PBRFinal-Flashlight.png). A cena principal e o player não são salvos.
As capturas usam as mesmas posições e parâmetros da revisão anterior.
Combate, FPS em hardware alvo e inspeção em monitores diferentes exigem playtest.
