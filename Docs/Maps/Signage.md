# Sinalização física do jogo

Revisão de **11/11/2026**. Abrange as 11 cenas em `Assets/_Project/Scenes/`, inclusive o mapa composto `Main/SampleScene`. O inventário em Unity encontrou 488 componentes TextMeshPro, dos quais 202 estavam visíveis antes da revisão. Textos de interface, teclados, bilhetes, pistas e letreiros institucionais foram distinguidos das placas de orientação.

## Padrão aplicado

- Placas funcionais usam a mesma fonte TextMeshPro já compartilhada no projeto e texto legível em caixa alta. Tinta clara: `(0,89; 0,90; 0,83)` sobre fundos escuros; tinta azul-escura: `(0,07; 0,16; 0,28)` sobre superfícies claras. Avisos restritos existentes mantêm o âmbar. O tamanho físico de cada placa foi preservado para não alterar fachadas ou passagens.
- Destinos e setores na mesma linha usam o separador `•`; setas ficam nas extremidades. Os quatro avisos do Bloco 4 foram resumidos para `ALA 1` a `ALA 4`, sem a contagem redundante de salas.
- Foram retiradas da exibição, sem excluir objetos, três placas genéricas `ACESSO / 01–03` do átrio `Cylinder`, pois as portas já têm placas próprias de destino, e o quadro genérico `SEÇÕES DO ACERVO / CONSULTE AS PLACAS NAS ESTANTES` da `CampusLibrary`, pois as faixas de cada estante já identificam o acervo. Seus fileIDs e dependências permanecem.
- Continuam visíveis as placas de salas, alas, sanitários, extintores, serviços, saídas, trânsito e restaurante. Números de teclado, pistas, bilhetes, painéis de computador, marcas `UniDoom`/`UNIMAR` e placas impressas em textura com acabamento envelhecido mantêm sua função e arte. As placas antigas de `Cylinder` e `Library` já compartilham a montagem de esmalte/metal, por isso não receberam materiais novos.

Os prefabs de cenário e as cenas independentes afetadas foram salvos com GUIDs, fileIDs e meshes preservados. `CampusExterior` continua em sua cena e prefab; `LawSchool` foi alterado nos dois assets independentes; `EntranceHallway.prefab` fornece as mesmas cores a `EntranceGarden` e ao mapa composto. As alterações não mudam portais, chaves ou scripts de gameplay.

**Validação desta rodada:** compilação no Unity 6000.5.7f1, carregamento das 11 cenas e conferência do inventário ativo após a edição: 200 textos visíveis, dois a menos que antes. As alterações em `LiberationSans SDF - Fallback.asset` e `Campus Library staff sign.mat` já presentes durante esta revisão foram preservadas. Play Mode, build, leitura visual em perspectiva e percurso com o jogador não foram executados.
