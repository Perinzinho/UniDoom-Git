# Interior da sala do diretor

Primeira versão 3D low poly, com texturas pixeladas e decoração inspirada na referência fornecida.

## Abrir e editar

- Sala existente: `Assets/prefabs/Scenario/DirectorRoom.prefab`.
- Móveis e decoração: `Assets/Art/DirectorOffice/HauntedOfficeInterior.prefab`.
- Cena independente de inspeção: `Assets/Art/DirectorOffice/DirectorOfficePreview.unity`.
- Materiais, texturas e malhas: subpastas de `Assets/Art/DirectorOffice`.
- Imagem da instância do mapa: `Tools/DirectorRoom/DirectorRoom-preview.png`.

O interior é um prefab aninhado. Mesa, cadeiras, estantes, arquivos, quadros, plantas,
janela decorativa e luminária têm grupos próprios para ajustar posição e rotação.
As partes pequenas de cada móvel foram unidas por material, totalizando 76 renderizadores.
Há colisões simples nos móveis grandes; a aproximação da porta permanece livre.

As posições, rotações, escalas e colisões originais da sala foram preservadas.
Apenas os materiais do piso, paredes e teto foram trocados, e o interior foi adicionado.
A janela vermelha é um painel decorativo: não há abertura nova na parede.
As alterações existentes da instância na `SampleScene` foram mantidas.
A iluminação final também depende das luzes e do ambiente da cena onde a sala for usada.

## Fonte e validação

`DirectorRoomInteriorBuilder.cs` contém a construção das malhas e das texturas,
sem dependências externas de modelagem. Fica fora de `Assets` para não executar
nem ser compilado no projeto principal. Para gerar em uma cópia isolada, colocá-lo
em `Assets/Editor` e executar pelo Unity 6000.5.7f1:

```
-batchmode -quit -executeMethod DirectorRoomInteriorBuilder.Build
```

`install_interior.py` insere o prefab aninhado sem reserializar os objetos antigos,
e confere os blocos originais de transformações e colisões antes de gravar.
O script instala primeiro na cópia `.utmp/DirectorRoomPreview`; `--install` copia
os assets validados para o projeto principal. Ele recusa uma instalação duplicada.

`DirectorRoomInteriorBuilder.VerifyAndRender` verifica os materiais e malhas,
a presença do interior no mapa e o corredor de acesso, e gera as imagens.
Não salva alterações na cena principal. A validação gráfica foi feita no editor;
movimentação do jogador e comportamento dos inimigos ainda precisam de playtest.
