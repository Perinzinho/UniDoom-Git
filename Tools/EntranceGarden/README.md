# Jardim e corredor gótico

Decoração 3D low poly com texturas pixeladas, inspirada na referência de jardim
gótico. O céu, a neblina, o ambiente e a luz direcional existentes foram mantidos.

## Arquivos

- `Assets/prefabs/Scenario/EntranceGarden.prefab`: prefab existente com a decoração aninhada.
- `Assets/Art/EntranceGarden/GothicGardenDecoration.prefab`: móveis, vegetação e detalhes.
- `Assets/Art/EntranceGarden/Materials`, `Textures`, `Meshes`: assets gerados.
- `Assets/Scripts/Scenario/EntranceGardenLayout.cs`: componente necessário para ajustar a decoração ao piso e às paredes da instância.
- `Tools/EntranceGarden/EntranceGarden-preview.png`: imagem do jardim no mapa, com o céu original.
- `Tools/EntranceGarden/EntranceGardenBuilder.cs`: fonte de geração, mantida fora de `Assets`.
- `Tools/EntranceGarden/install_garden.py`: instalador opcional que preserva os blocos originais do prefab.

O layout contém caminho de pedra, canteiros, sebes, ciprestes, árvores secas,
grades, pilares com lanternas, guardiões de pedra, bancos, urnas, rosas e folhas caídas.
Cada elemento possui um grupo próprio. Os detalhes pequenos foram unidos por material.
Para alterar a distribuição de forma permanente, ajustar `placements/anchor`,
`scale` e `yaw` no componente `EntranceGardenLayout` do prefab de decoração.

## Dimensões preservadas

O piso, as paredes e seus colliders não foram movidos nem redimensionados.
Os materiais do piso e da parede do prefab foram trocados e foi adicionado um
prefab filho. As faces decorativas de alvenaria ficam dentro das paredes existentes.
O componente de layout altera apenas os novos objetos e mantém o caminho livre.
Ele compensa a escala não uniforme da instância para evitar árvores e pilares achatados.

A instância atual no mapa oferece cerca de 7,17 m entre as paredes e 24,60 m
de comprimento. O caminho central tem aproximadamente 3,73 m. O prefab original,
mais estreito, mantém cerca de 1,51 m de passagem com a decoração ajustada.

As configurações de céu/ambiente não são escritas pelo componente nem pelo gerador.
As lanternas adicionam somente luzes locais. A cena principal não foi salva pelo processo.

## Gerar e validar

Em uma cópia isolada do projeto, colocar `EntranceGardenBuilder.cs` em `Assets/Editor`
e incluir o componente `EntranceGardenLayout.cs` com seu `.meta`. Executar no Unity:

```
-batchmode -quit -executeMethod EntranceGardenBuilder.Build
```

O instalador usa a cópia `.utmp/DirectorRoomPreview`. Sem argumentos, instala nessa
cópia. `--install` copia os assets validados para o projeto principal. Ele recusa uma
instalação duplicada e compara os blocos originais de transformações, meshes e colisões.

`EntranceGardenBuilder.VerifyAndRender` verifica referências, limites dos colliders
e passagem central no mapa e no prefab. Renderiza sem alterar o céu ou salvar a cena.
A circulação do jogador e a navegação dos inimigos ainda precisam de playtest.

Para versionar a mudança, incluir o prefab modificado, toda a pasta
`Assets/Art/EntranceGarden` e seu `.meta`, e o componente em `Assets/Scripts/Scenario`
com os respectivos `.meta`. A pasta `Tools/EntranceGarden` guarda a fonte e documentação.
