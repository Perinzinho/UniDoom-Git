# Library — acabamento universitário e luzes defeituosas

Troca das superfícies principais de parede, pilares, vigas e teto do `Library.prefab`, preservando o layout, fileIDs, transformações, gameplay e assets anteriores. A linguagem institucional usa reboco marfim envelhecido, rodapés verdes existentes e teto claro dividido por perfis em módulos de 60 × 120 cm. Algumas placas têm manchas localizadas de infiltração. Os danos, livros e entulho da revisão anterior continuam presentes.

Três fluorescentes da biblioteca piscam de forma independente, com oscilações e apagões breves. Reutilizam `TotalChaosMaps.TCFlickerLight`, com emissão dos tubos sincronizada por `MaterialPropertyBlock`. As outras fontes continuam estáveis para permitir circulação. Nenhum script de runtime compartilhado foi alterado; o gerador fica fora de Assets.

Texturas PNG PBR 2K: [Painted Plaster Wall](https://polyhaven.com/a/painted_plaster_wall) de Amal Kumar e [White Plaster 02](https://polyhaven.com/a/white_plaster_02) de Rob Tuytel, ambas [CC0](https://polyhaven.com/license). Downloads oficiais, autores e SHA256 em [sources.json](sources.json). Albedos usam sRGB; normais e máscaras são lineares, com mipmaps, filtragem trilinear e compressão. ARM foi empacotado para o shader existente; não há textura complexa sintetizada para substituir esses mapas reais.

Novos assets exclusivos ficam em subpastas `CampusFinish` na Library. Dois novos cubemaps de reflexão acompanham a troca dos acabamentos. Prefab anterior e meta estão nesta pasta; `before-manifest.json` registra os hashes antes da edição. Nada foi apagado.

## Validação

Unity 6000.5.7f1 / URP 17.5.0, em cópia isolada do projeto. `CampusLibraryFinish.BuildAndVerify` salva os assets, recalcula reflexos, valida referências e a instância no mapa e roda o teste de circulação com CharacterController em Play Mode. `CampusFlickerCheck` mede a variação real de intensidade das três fontes e confere que a emissão dos tubos acompanha as mudanças.

Evidências: [flicker-validation.txt](flicker-validation.txt), [play-validation.txt](play-validation.txt), [validation.txt](validation.txt), [instalação](installed-validation.json), [log Unity](unity-validation.log) e [captura no mapa](InMap-Quarantine.png). O log pode conter o erro preexistente do indexador `UnityEditor.Search`, também registrado antes desta revisão; os marcadores dos testes verificam separadamente os resultados do prefab. Não foram executados build standalone, benchmark de FPS ou teste completo de combate.

Para repetir a geração, sincronize o prefab e mapa atuais em `.utmp/LibraryPreview`, baixe os recursos com `download.py`, copie `CampusLibraryFinish.cs` para `Assets/_TemporaryTools/Editor` da cópia isolada e execute `CampusLibraryFinish.BuildAndVerify` sem `-quit`, com as ferramentas de verificação da revisão anterior. Execute `install.py` somente depois dos quatro marcadores PASS. Ele instala metas antes dos assets e troca o prefab por último, validando IDs, transformações, hashes e novos GUIDs.

## Tamanho em disco

A medição inicial está em `size-before.json`. São tamanhos lógicos em GB decimais, não a soma de um build distribuído: Assets 1,67 GB, cache Library 3,39 GB, cópias/temporários `.utmp` 18,64 GB e pasta de desenvolvimento aproximadamente 25,67 GB, incluindo Git, ferramentas e arquivos auxiliares. O tamanho do jogo instalado exige um build real, que não foi feito nesta tarefa.

Após a integração dos novos PNGs, [size-after.json](size-after.json) registra: **Assets 1,82 GB**, **pasta completa 26,08 GB**, dos quais **18,87 GB** são `.utmp` e **3,39 GB** são cache da Unity. Os arquivos anteriores e backups foram preservados.
