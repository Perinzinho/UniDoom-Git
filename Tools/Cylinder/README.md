# Cylinder — pátio circular abandonado

## Revisão atual: portas discretas e vestígios das criaturas

As **43 portas lacradas** continuam sendo PNGs estáticos sobre paredes sólidas,
agora **sem X, tábuas ou maçanetas**, com três acabamentos: metal esverdeado com
veneziana, painel oxidado e chapa cinzenta. Apenas as três portas funcionais
mantêm maçanetas, indicadores vermelhos e abertura por aproximação. A textura
principal conserva seu GUID e o gerador original também deixa de desenhar os X.

O novo grupo `Abandoned circular atrium/Timeworn atrium stories` acrescenta uma
cantina encerrada com refeições estragadas, bandejas, embalagens, bagagem,
papéis e louça quebrada; rádio sem resposta e mapa do campus; quadro de
desaparecidos; máquina de venda avariada; bancos gastos e relógio parado às 03:17.
Pegadas com quatro garras, sangue, sulcos nas paredes e ovos nas jardineiras
conduzem à carapaça abandonada de uma criatura na galeria. Infiltrações, reboco
descascado, conduítes rompidos e isolamento pendurado no teto reforçam o tempo
e a deterioração. A carapaça e os ovos são elementos de cenário; não criam IA.

Algumas luminárias ficam apagadas e a cantina tem luz mais fraca. Pontos de luz
quente destacam pistas de exploração, preservando a paleta e o acabamento
pixelado das outras salas. Os detalhes usam malhas agrupadas por material e
texturas Point com limite de 128 px. Rastros, ovos, carapaça e pequenos destroços
não adicionam colisões; bancos, máquina e posto de rádio ficam junto às paredes.
As placas usam letras de pixels feitas em malha, com oclusão pelas paredes e
acabamento legível sob a luz baixa. Os textos antigos do cilindro são substituídos
visualmente por essa mesma tipografia, mantendo seus objetos e IDs anteriores.

`CylinderDecayBuilder.cs` aplica essa revisão sobre o prefab atual numa cópia
isolada `.utmp/BasementPreview`; copie também `CylinderDecayVerifier.cs` e
`CylinderBuilder.cs` para `Assets/Editor`. Execute `CylinderDecayBuilder.Build`
no Unity 6000.5.7f1, com log em `.utmp/Cylinder-decay-build.log`.

`install_cylinder_decay.py` usa `.utmp/Cylinder-decay-before` e recusa alterações
concorrentes. Sem argumentos, prepara o prefab preservando os blocos e IDs
anteriores. Execute `CylinderDecayVerifier.Verify` sobre essa versão, com log em
`.utmp/Cylinder-decay-verify.log`; `--install` instala a arte e as prévias após o
sucesso. A cena principal, o player, o porão e a sala de aula permanecem idênticos
ao snapshot. Os materiais e a geometria anteriores são mantidos, com alterações
restritas às folhas estáticas, iluminação e textos da cantina.

A verificação percorre os dois circuitos e a entrada com o CharacterController
real, testa as três portas fechadas e abertas, confere os 80 objetos apoiados na
cantina e a cobertura completa do teto no prefab e com a transformação do mapa.
`decay-validation.txt` registra os resultados. As novas imagens incluem
`Cylinder-RadioInMap.png`, `Cylinder-CreatureTracesInMap.png` e
`Cylinder-SealedDoorInMap.png`. Os testes são de edição; combate e ritmo de
descoberta continuam sujeitos a playtest.

O prefab `Assets/prefabs/Scenario/Cylinder.prefab` agora contém um átrio oval
inspirado nas referências: cantina central, mesas e cadeiras, três portas funcionais,
galeria perimetral, pilares com jardineiras, treliças radiais, cobertura completa,
grades, hera pendente, folhas caídas, drenagem e poças.
O acabamento usa cópias dos materiais existentes: pedra, ferro, ferrugem, aço,
piso e emissivos do Basement; reboco e poças da ClassRoom; madeira, vermelho gasto
e folhas da DirectorOffice; hera e folhas caídas da EntranceGarden.
As cópias acompanham a revisão atual dessas salas, com acabamentos derivados
da sala do diretor. A conferência está em [RoomStyle](../RoomStyle/README.md).
As texturas usam filtro Point e as propriedades URP dos acabamentos são preservadas.
As luzes locais usam o tom quente da sala do diretor e vermelho nas portas. A vegetação fica
agrupada em quatro setores por material para reduzir os renderizadores.

O piso desta revisão tem concreto rachado no corredor externo e mosaico gasto
em faixas curvas ao redor da cantina, com trechos faltando. Novas texturas
pixeladas mantêm o filtro Point e a paleta das salas. Manchas irregulares de
umidade e poeira, fragmentos de piso, tábuas caídas, cadeiras tombadas e mato
junto às paredes reforçam o abandono. A colisão do piso permanece contínua.

Tomando a entrada sul como referência, há uma porta à esquerda, outra à direita
e uma no fundo, atrás da cantina. Elas têm folha móvel, batente, maçaneta,
indicador vermelho e `OpenDoorWithKey`, usando a abertura por aproximação existente,
sem exigir chaves. As outras 43 portas são imagens planas de folhas lacradas,
sem X, maçanetas, triggers ou componentes de interação. A parede
atrás dessas imagens continua sólida. Apenas as três portas interagíveis têm
luz vermelha ligada; as luminárias das portas lacradas ficam apagadas.

As 16 máquinas, 16 urnas e 48 xícaras da cantina agora apoiam diretamente sobre
uma bancada contínua, com armários sob o tampo. A cobertura principal fecha em
360 graus, incluindo o centro, e vai até além das paredes, com colisão.

## Escala e circulação

- Base original de aproximadamente 57 × 41 m, com o posicionamento original.
- Player existente: CharacterController com cerca de 2,12 m de altura e 0,47 m de diâmetro.
- O grupo `Abandoned circular atrium` compensa a escala Y 0,2 da raiz: seu acabamento usa metros reais.
- Corredor externo com aproximadamente 4 m de largura no eixo menor.
- A entrada sul permanece aberta, com altura livre de 3,6 m. Três portas interagíveis ocupam as laterais e o fundo.
- Cada porta deixa cerca de 1,85 m de largura e 3,25 m de altura, com colisão acompanhando a folha.
- Cobertura a aproximadamente 6,8–8,6 m; cantina com tampo a 1,19 m e topo a 3,24 m.
- Jardineiras, pilares, paredes, grades, mesas e quiosque possuem colisões simplificadas.
- A galeria superior é um elemento arquitetônico; a circulação validada fica no térreo.

O piso original conserva seus IDs, dados ProBuilder e colisão. A parede e o teto
antigos ficam desativados como referência; a nova estrutura está agrupada sob
`Abandoned circular atrium`. O GUID do prefab é preservado. Nenhuma cena de jogo
é alterada pelo instalador. A instância existente em `SampleScene` continua
referenciando o mesmo GUID do Cylinder.

## Inspeção e validação

Abra `Assets/Art/Cylinder/CylinderPreview.unity`. As imagens `Cylinder-Player.png`,
`Cylinder-Walkway.png`, `Cylinder-Overview.png`, `Cylinder-Canteen.png`,
`Cylinder-ActiveDoor.png` e `Cylinder-Floor.png` estão nesta
pasta. O ambiente e a luz de inspeção são configurações de cena, seguindo a
apresentação do basement; as luzes locais pertencem ao prefab.

O gerador compila no Unity 6000.5.7f1 e usa o CharacterController real para
percorrer dois circuitos completos e a entrada sul. Verifica exatamente três
portas funcionais, a detecção do player por aproximação, passagem com a folha
aberta e bloqueio com ela fechada. A animação é colocada no fim de curso para
testar a geometria de forma determinística no editor. Também conta as 43 imagens
sem interação e verifica o apoio dos 80 objetos da cantina com
raycasts e a cobertura do teto com 384 raios verticais. Verifica materiais e
malhas antes de renderizar. Os testes rodam em modo de edição;
movimentação em Play Mode e balanceamento de combate precisam de playtest.
O instalador também verifica as referências dos assets no projeto final.

## Reprodução

Use o snapshot original `.utmp/Cylinder-original.prefab` em uma cópia isolada
do projeto. O gerador foi executado em `.utmp/BasementPreview`, aproveitando os
pacotes e texturas existentes. Copie `CylinderBuilder.cs` para `Assets/Editor`
da cópia e execute:

```
Unity.exe -batchmode -quit -projectPath <copia> -executeMethod CylinderBuilder.Build -logFile <log>
```

`Build` recria apenas `Assets/Art/Cylinder` na cópia isolada e exige o prefab
original. `CylinderBuilder.Verify` repete a validação e os renders sobre o prefab
já gerado. `CylinderBuilder.UpdateDoorLighting` aplica somente a revisão das
luzes sobre o prefab gerado e atualiza as prévias. Após o sucesso, execute
`python Tools/Cylinder/install_cylinder.py --replace --snapshot .utmp/Cylinder-before-door-lights.prefab`
para esta revisão. O instalador compara o prefab com esse snapshot
antes de sobrescrever; sem `--replace`, exige
o original. Remove arquivos gerados obsoletos somente dentro de `Assets/Art/Cylinder`.
