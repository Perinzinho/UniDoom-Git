# Geometria e materiais inspirados no Total Chaos original

A atmosfera e a amostragem atuais foram atualizadas pela
[skill Total Chaos Maps](../TotalChaosSkill/README.md). Essa revisão mantém a
geometria desta pasta, mas aplica Point/128 px, névoa regional, luz escassa e
pós-processamento. Os valores de materiais descritos abaixo documentam a base
anterior à aplicação da skill.

Esta revisão aplica as referências do **mod gratuito para GZDoom**, publicado no
ModDB, à sala de aula, ao porão e ao cilindro. A referência foi estudada antes da
implementação. As malhas e texturas desta revisão são próprias do UniDoom.

## Estudo de referência

- [Página do mod no ModDB](https://www.moddb.com/mods/total-chaos): cenário industrial
  abandonado de Fort Oasis, exploração e pistas ambientais; modelos 3D e texturas
  com mais detalhe do que no Doom original.
- [Galeria oficial](https://www.moddb.com/mods/total-chaos/images) e
  [Level detail overhaul](https://www.moddb.com/mods/total-chaos/images/level-detail-overhaul):
  arquitetura deteriorada, curvas orgânicas, entulho desigual e luz concentrada.
- [Captura dos serviços industriais](https://media.moddb.com/images/members/1/314/313486/profile/tc3.png):
  tubos arredondados, conexões, flanges aparafusadas, sacos amassados, umidade e
  camadas de sujeira. A geometria ajuda a reconhecer a função de cada objeto.
- [Publicação do autor](https://wadaholic.wordpress.com/total-chaos/) e sua
  [captura de corredor de 2016](https://wadaholic.wordpress.com/wp-content/uploads/2016/10/01.jpg):
  instalações envelhecidas, superfícies irregulares e iluminação com forte
  contraste, mantendo o percurso legível.

A aplicação é uma interpretação visual dessas capturas. O trabalho não pretende
reproduzir as mecânicas nem a iluminação volumétrica do mod. Não utiliza assets
extraídos dele nem referências do remake comercial.

## Aplicação

- **Cilindro:** 46 folhas com modelo verde de painéis e veneziana, largura de
  **1,85 m** e altura de **3,25 m**. As 43 decorativas têm o mesmo acabamento,
  moldura e tamanho; não têm X, maçaneta ou interação. As três funcionais mantêm
  maçanetas, comportamento, colisão móvel e indicadores existentes. Acrescenta
  tubulações curvas, sacos deformados, entulho fraturado, toldo com dobras e
  carapaça segmentada com membros curvos afilados.
- **Sala:** bordas chanfradas em carteiras, cadeiras e equipamentos; radiador com
  tubos curvos, cabo pendente, tecido com dobras sobre os corpos, lona caída e
  fragmentos de alvenaria com contorno irregular. Escala universitária e corredores
  existentes preservados.
- **Porão:** barris com perfil, aros e amassados; tubos curvos com flanges,
  parafusos sextavados e volantes de válvula; cabos com flecha, entulho irregular,
  mortalhas dobradas, costelas curvas e espinhos afilados na sala final.
  As dimensões ampliadas e a estrutura de combate são mantidas.

O acabamento usa oito conjuntos próprios de 512 px: albedo, normal e mapa de
metal/suavidade para alvenaria, metal, ferrugem, pintura, madeira, tecido, sangue e
matéria orgânica. A paleta deriva dos materiais atuais; emissivos, sinalização,
vegetação e mosaico mantêm os acabamentos existentes. Mipmaps e filtragem bilinear
reduzem a cintilação dos detalhes à distância. As novas peças decorativas são
agrupadas por material e não acrescentam colisão.

## Geração e instalação

1. Preserve os assets atuais em `.utmp/TotalChaos-before/Assets` e seu manifesto
   SHA-256 em `manifest.json`. Sincronize esses assets numa cópia isolada do projeto.
2. Copie os três arquivos C# desta pasta e os verificadores das salas para
   `Assets/Editor` da cópia `.utmp/BasementPreview`.
3. Execute `TotalChaosStyleBuilder.Build`, com log `.utmp/TotalChaos-build.log`.
   O gerador restaura os cinco prefabs do snapshot antes de cada geração para
   evitar aplicar chanfros duas vezes ou duplicar as peças.
4. Execute `python Tools/TotalChaosStyle/install_total_chaos_style.py` para preparar
   os prefabs com os IDs e blocos originais preservados.
5. Valide a versão preparada em processos Unity separados:
   `TotalChaosStyleVerifier.VerifyVisuals` (`TotalChaos-verify.log`),
   `ClassRoomHorrorVerifier.VerifyAndRender` (`TotalChaos-classroom.log`),
   `BasementFinalVerifier.Verify` (`TotalChaos-basement.log`) e
   `CylinderDecayVerifier.Verify` (`TotalChaos-cylinder.log`).
6. Execute o instalador com `--install`. Ele exige os logs sem erros, recusa
   alterações concorrentes e instala somente assets próprios referenciados.

O instalador modifica apenas materiais, habilitação de renderizadores, referências
de malha e listas de filhos nos componentes anteriores. Colisores, transforms,
scripts de gameplay e todos os IDs anteriores permanecem intactos. A cena
principal, o player e o prefab externo da sala permanecem idênticos ao snapshot.
Os testes de circulação e portas usam o CharacterController real em modo de
edição; combate, desempenho em hardware alvo e ritmo de exploração precisam de
playtest.

As faces planas de malhas anteriores também recebem UVs em escala métrica para
evitar esticar uma textura pequena sobre uma parede inteira. A geometria e a
colisão dessas superfícies não mudam.

As imagens `TotalChaos-*.png` mostram os resultados na posição real do mapa.
Os outros cenários ficam ocultos apenas durante a captura; a validação de física
mantém as colisões do mapa. Nenhuma cena principal é salva pelo gerador.

O resultado da instalação e das quatro verificações está em
[validation.md](validation.md).
