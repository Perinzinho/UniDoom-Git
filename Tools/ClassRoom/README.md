# Sala universitária abandonada

A revisão está no prefab aninhado `Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab`,
usado por `Assets/prefabs/Scenario/ClassRoom.prefab` e pela `SampleScene`.

As carteiras passam a ter tampo de **1,36 × 0,82 m**, a **1,066 m** do chão.
O assento das cadeiras fica a **0,589 m**. A escala considera o CharacterController
do player, que tem **2,121 m** de altura; mesas e cadeiras do professor também
acompanham esse ajuste. As 42 carteiras são redistribuídas para acomodar o novo
tamanho e permitir circulação ao redor dos obstáculos.

A sala ganha uma aula de Física II no quadro, aviso de interdição do campus,
computador antigo no púlpito, notebooks, manuais, café esquecido, projetor
danificado e tela rasgada. O abandono aparece no mofo, caixa elétrica queimada,
cabos soltos, impactos na parede, móveis tombados e entulho.

Poças de sangue, respingos, marcas de mãos, pegadas e rastros de arrasto conduzem
a três corpos adultos vestidos, com a cabeça inteiramente coberta e sem detalhes
faciais. Um deles está sob um lençol. Algumas luminárias ficam apagadas, com
iluminação quente pontual e uma luz vermelha de quarentena. As texturas e materiais
mantêm o acabamento pixelado já usado no projeto; a geometria dos corpos é simples
e facetada. Corpos, sangue e pequenos destroços não adicionam colisões.

## Geração e validação

Copie `ClassRoomHorrorBuilder.cs` e `ClassRoomHorrorVerifier.cs` para `Assets/Editor`
da cópia isolada `.utmp/BasementPreview`, sincronizada com os assets atuais.
Execute no Unity 6000.5.7f1:

```text
-batchmode -quit -executeMethod ClassRoomHorrorBuilder.Build
```

O gerador aplica escalas absolutas, substitui apenas o grupo de novos detalhes e
reutiliza os assets de materiais e malhas. A porta e a estrutura da sala são
mantidas. O verificador confere materiais, dimensões, rota com o CharacterController
real em ida e volta, passagem de uma cápsula de 1 m, corredor frontal e giro da
porta em ambas as direções. Também confere a herança dos detalhes na cena principal.
Essa validação em modo de edição não substitui um playtest do combate.

`install_classroom_horror.py` usa o snapshot `.utmp/ClassRoom-horror-before` e
recusa mudanças concorrentes. Sem argumentos, mostra os arquivos a instalar;
com `--install`, transfere a arte validada e as prévias. Os blocos YAML e IDs
preexistentes são preservados, com alterações restritas a transformações,
materiais das luminárias, iluminação e textos. A cena principal, o player e o
prefab externo da sala permanecem byte a byte como no início desta revisão.
Antes da instalação, `--stage` grava os prefabs preservados na cópia isolada.
Execute `ClassRoomHorrorVerifier.VerifyAndRender` sobre essa versão, com log em
`.utmp/ClassRoom-horror-final.log`; o instalador exige que essa conferência passe.

As imagens `ClassRoom-player-view.png`, `ClassRoom-preview.png`,
`ClassRoom-aftermath.png` e `ClassRoom-blood-trail.png` mostram o resultado.
`Assets/Art/ClassRoom/ClassRoomPreview.unity` permite inspecioná-lo no editor.
