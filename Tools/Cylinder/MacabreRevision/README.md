# Cylinder: rádio, sinalização PNG e vestígios nas paredes

Revisão do prefab `Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab` sobre o resultado de `CanteenDoors`. Não reorganiza os assets existentes.

## Resultado visual

- 47 placas com PNGs pretos originais do OpenMoji sobre esmalte marfim envelhecido, bordas metálicas e parafusos. Dois pequenos PNGs adicionais acompanham as folhas móveis da biblioteca e da diretoria. Permanecem sala de aula à direita (+X), biblioteca à esquerda (-X), diretoria atrás (-Z) e entrada frontal maior.
- Rádio militar detalhado `vintage_radio_transceiver` do Poly Haven, de Mateusz Sadek, com antena, microfone, cabos e manipulador Morse. Sai da estação do setor 7 e aparece numa bancada no setor 34, próximo da biblioteca, sob uma pequena luminária protegida. A estante e o mapa antigos permanecem.
- Cinco balcões da cantina recebem micro-ondas, chaleiras, tigelas e latas no lugar das máquinas repetidas. A ilha central de preparo e a estante interna acrescentam armazenamento e utensílios. Materiais PBR dos modelos, com vidro escuro nos visores dos aparelhos.
- Sangue seco, marcas de mãos, desenhos a carvão e a inscrição `ELE AINDA ESTÁ AQUI` estão nos pixels de um atlas com alpha. Aplicação localizada nas paredes, com materiais iluminados pela cena. Sem componentes de texto, Canvas, emissão ou letreiros flutuantes.

As manchas maiores e um desenho aparecem no trecho de parede da antiga abertura secundária, ao lado da entrada. Outros vestígios ficam no percurso de fundo e perto do rádio. O material de pigmento desativa a preservação de reflexos especulares da transparência URP para que a área vazia do atlas não produza um retângulo visível. As molduras estáticas das placas são combinadas em oito setores, preservando as imagens e a leitura individual de cada porta.

## Fontes e licenças

Modelos e mapas: [Poly Haven / CC0](https://polyhaven.com/license). Páginas: [rádio](https://polyhaven.com/a/vintage_radio_transceiver), [chaleira](https://polyhaven.com/a/vintage_electric_kettle), [micro-ondas](https://polyhaven.com/a/vintage_microwave), [latas](https://polyhaven.com/a/russian_food_cans_01), [tigela](https://polyhaven.com/a/wooden_bowl_01).

PNGs: [OpenMoji](https://openmoji.org/), CC BY-SA 4.0. Imagens originais sem edição raster. Créditos e obrigação de conservar a licença das imagens em [Attribution.txt](Attribution.txt), também copiado para a pasta das texturas no projeto. Downloads individuais, autores, bytes e SHA-256 em [asset-sources.json](asset-sources.json). Os PNGs e modelos não têm marca d'água.

`WallResidueAtlas.png` é arte original gerada com a skill imagegen, modo **built-in image_gen**, fundo transparente preservado. Prompt exato em [image-prompt.txt](image-prompt.txt). Salvo no projeto em `Assets/_Project/Scenes/Cylinder/Textures/MacabreRevision/WallResidueAtlas.png`. Os recortes são feitos pelas UVs dos meshes; a imagem original não foi repintada por scripts.

## Preservação e reprodução

Backup de entrada e inventário SHA-256 em `.utmp/CylinderMacabreBefore`. Geração na cópia `.utmp/CylinderDetailPreview`, com código temporário em `_TemporaryTools/Editor`. Código-fonte mantido nesta pasta `Tools`; nenhum novo gerador runtime é instalado.

`prepare.py` fotografa o estado inicial e sincroniza a cópia isolada. `download_assets.py` baixa e verifica PNG/FBX contra os manifests oficiais. `CylinderMacabreAudit.Run` importa e inventaria os materiais do FBX. `CylinderMacabreBuilder.Build` gera resultados persistentes. `CylinderMacabrePolish.Run` ajusta orientação e apresentação. `install.py` restaura os blocos existentes de física, scripts e MeshFilter, conservando todos os fileIDs e GUIDs; apenas filhos novos, materiais e visibilidade de renderers mudam nos objetos antigos.

As versões de meshes que removem o rádio simples e cinco aparelhos repetidos são assets novos, com renderers novos. Os MeshFilters originais e seus assets permanecem intactos; seus renderers substituídos ficam desabilitados. A estante/mapa antigos continuam visíveis e com a colisão original.

## Validação

Os relatórios `structure-validation.json`, `edit-validation.txt`, `play-validation.txt` e `installed.json` registram os testes e os arquivos realmente aplicados. As capturas PNG nesta pasta mostram o resultado renderizado na Unity.

Instalação concluída: 800 arquivos copiados, incluindo assets novos, seus `.meta` e o prefab. Todos conferidos por SHA-256 após a cópia. Nenhum outro asset existente do inventário de entrada mudou. O prefab instalado é byte a byte igual à versão testada na Unity isolada.

Unity 6000.5.7f1 / URP 17.5: compilação e carregamento do prefab final aprovados; referências, meshes e materiais válidos. Os 5.570 fileIDs de entrada permanecem, com 1.863 blocos novos. Apenas 54 blocos antigos mudam, dos tipos Transform (filhos) e MeshRenderer (visibilidade/material); física, scripts e bindings de MeshFilter antigos preservados integralmente.

Testes concluídos: quatro portas por aproximação com passagem aberta e bloqueio fechado; dois circuitos completos e entrada sul com o CharacterController real, em duas posições do prefab; 80 apoios de balcão; 384 raios de cobertura; seis áreas de spawn livres; 49 PNGs vinculados; sete aplicações do atlas junto a superfícies sólidas; pés dos modelos importados apoiados. Em Play Mode, a entrada abre, fecha e reabre; seis inimigos surgem após entrar e não duplicam na reentrada.

Durante a inicialização do Play Mode isolado, a Unity voltou a registrar a exceção interna de indexação `UnityEditor.Search.SearchDatabase` já observada nas revisões anteriores dessa cópia. O teste prosseguiu, registrou `CYLINDER_MACABRE_PLAY_PASS` e encerrou com código 0. Esse registro não foi atribuído às mudanças de arte.

Não foram executados build de distribuição, combate completo no mapa ou profiling no hardware-alvo. As contagens de geometria não equivalem a ganho de FPS.

Contagem do prefab inteiro: 796 MeshRenderers visíveis e 776.191 triângulos instanciados, contra 679 / 691.129 na entrada desta revisão. O agrupamento das novas molduras reduziu a primeira versão de 945 para 796 renderers, sem mudar os triângulos. Aumento de detalhe e de custo geométrico é esperado; desempenho em ms/FPS permanece sem medição no alvo.

Revisão visual: escala e apoio dos objetos conferidos, desgaste localizado, materiais distinguíveis, fontes de luz identificáveis e circulação preservada. As marcas a carvão são discretas nos setores escuros; as manchas próximas da entrada e o rádio iluminado criam pontos de descoberta. Nenhuma aprovação de desempenho foi inferida de capturas do Editor.
