# Bloco 4 — Tecnologia da Informação

Atualizado: **2026-11-08**, revisão de [correcoesbloco4.md](correcoesbloco4.md). Fontes normativas: `.codex/PROMPT_MESTRE.md` e `total-chaos-visual-direction` / `.codex/SKILL.md`. As medidas e os testes das revisões anteriores são históricos.

## Leitura rápida

- **Acesso à biblioteca (09/11/2026):** uma chave em aço com etiqueta azul, ID `campus-library-block-4`, na bancada da sala 423 atrás da porta com senha. Puzzle resolvido e porta totalmente aberta continuam obrigatórios; trigger de coleta preservado.
- Antigas chaves de cela **7/8/12/13 retiradas**: a recompensa original da 423 foi convertida para acesso à biblioteca; os três pickups adicionais ficaram inativos. Nada foi transferido para a biblioteca. [Contrato de acesso](CampusLibrary.md#acesso-com-duas-chaves--09112026).
- Cena `Assets/_Project/Scenes/CampusBlock4/CampusBlock4.unity`; prefab `Prefabs/CampusBlock4.prefab` na mesma pasta. Sem nova cena/menu.
- **Salas 401–424, 426 e 428 em uso**; 425/427 inativas. **22 laboratórios**, incluindo a antiga sala de auditório 424, copa 401, **secretaria 404**, coordenação 410 e periféricos 419. Ambientes de apoio preservados por confirmação do usuário; quatro banheiros compartilhados entre os corredores.
- Módulos preservados: largura **13,5 m**, profundidade externa **17 m**, interna **11,75 m**, conforme LawSchool. Os novos laboratórios das extremidades usam 13,5 × 17 m, rotacionados para abrir para os corredores. Pé-direito aproximado **4,06 m**.
- Hall de **15 m** e quatro corredores de **8 m**, cruzamentos em `z = ±16,25`. **426/428 na fileira externa em frente aos banheiros**, com centro `x = 54,75`, frentes `z = −20,25 / +20,25`, footprint `x = 48…61,5`, profundidade até `z = ±37,25`. Antigos acessos nas extremidades fechados em `x = ±71,75`. O hall ainda termina em `z = 20,25`; a espera posterior não foi recriada.
- **79 bancadas contínuas de quatro computadores**: 316 estações de alunos, mais mesas docentes. Três fileiras nas salas internas e quatro nas externas, incluindo a 424 convertida em laboratório. Tampos de **7,6 × 0,95 m**, topo a 0,90 m, com estrutura metálica, cadeiras, mini-PCs e canaletas. Mesas individuais antigas dos alunos inativas; 11 bancadas dos antigos laboratórios 404/425/427 também inativas.
- Modelos novos de monitor 24 polegadas, teclado e mouse são CC0, com meshes/materiais compartilhados entre as instâncias. Professores e apoio reutilizam os modelos existentes. Fonte: `Licenses/AssetSources/CampusBlock4/corrections-models.json`.
- Paleta laranja/azul conservada. Piso terrazzo e reboco padronizado reutilizam a base visual de Direito, em materiais exclusivos do Bloco 4. Paredes contínuas junto às lousas; janelas transparentes de observação restauradas nas frentes, ao lado das portas.
- Quatro longarinas e quatro vasos do hall encostados às paredes, com folga geométrica de **15 mm**. Bancos mantêm orientação e escala; vasos girados para acomodar as folhagens. O vaso antes diante da secretaria foi levado para `z = −25,5`, junto à parede oeste perto da entrada; balcão, foto/símbolos e cruzamentos dos corredores livres.

- Pisos: 129 colisores ativos com tag `Eco` e superfície `Interior`. Energia: primeiro evento em 6–10 s, seguintes em 12–22 s, apagão de 2–4 s; 199 luzes e 182 difusores. Chave de acesso à biblioteca somente na recompensa do puzzle da 423; antigas chaves de cela retiradas. Contrato compartilhado: [PrisonCellKeys.md](PrisonCellKeys.md).

## Portas, catracas e identificação

Portas numeradas com vão nominal de **1,75 × 3,075 m**, folhas de **1,746 × 3,071 m**, folga de **2 mm** por borda, batentes e vedação visual. A secretaria mantém a porta original da 404 para funcionários. Registros/fileIDs dos objetos antigos mantidos; geometrias e colisores substituídos ficam inativos. Duas caixas antigas do perímetro foram desativadas porque bloqueavam as novas portas das extremidades; após retirar 425/427, novos fechamentos sólidos vedam os finais oeste.

Na terceira lista, 426/428 passaram a abrir para as fileiras externas e os acessos anteriores nos finais leste também foram fechados. A barra restante da antiga porta da secretaria na entrada foi desativada, preservando parede e barreira das catracas.

Catracas em `z = −42,15`: quatro pedestais, três passagens e barreira fixa até as paredes. `OpenDoorWithKey.openingSideAxis` mantém X como padrão para portas existentes; nas catracas usa o eixo de passagem, com sentido escolhido pela aproximação do jogador. Não houve travessia em Play Mode nesta revisão.

Frentes em uso identificadas por **401–424, 426 e 428**, sem placas de matérias. Textos estáticos permanecem aplicados por atlas/UVs. Placas gerais do bloco e de passagem livre continuam retiradas. Lousas novas têm **5,6 × 1,65 m**, centro a **2,45 m** e parede posterior fechada. Vidros frontais entre 1,05 e 2,85 m de altura permitem observar os interiores, com caixilhos e colisores.

## Secretaria, periféricos e apoio

Secretaria **substitui o laboratório 404**, conforme a orientação final do usuário. Usa o módulo interno de **13,5 × 11,75 m**, em `x = −21…−7,5`, `z = −12,25…−0,5`. O guichê de LawSchool foi reutilizado e voltado diretamente para o hall de 15 m, depois das catracas, em `x = −7,5`, centro `z = −6,375`; não há corredor de atendimento. Entrada de funcionários pela porta original da 404 no primeiro corredor. Mobiliário administrativo reutiliza prefabs existentes; retrato na parede interna oeste preservado; a quinta pista agora participa dos destinos nos laboratórios. Assentos do hall afastados da frente do balcão. Antiga secretaria junto à entrada e corredor de serviço inativos. As salas 416 e 422 continuam sendo laboratórios.

Sala 419 ocupa todo o módulo interno de **13,5 × 11,75 m**, sem as divisórias que limitavam o depósito a uma fração da sala. Frente liberada: estantes dianteiras, móveis do antigo depósito compacto e placa de cabos/periféricos fora de uso. Duas estantes posteriores e bancadas de diagnóstico mantidas; caixas empilhadas, cabos, monitores, teclados, notebooks e ferramentas acumulados no fundo como equipamentos em triagem. Modelos e materiais existentes reutilizados. Copa 401 e coordenação 410 mantidas.

Quatro banheiros usam os módulos de LawSchool: um par masculino/feminino em cada lado do edifício, quatro cabines por banheiro e acesso pelos dois corredores. Femininos de **13,5 × 24,5 m**, masculinos de **9,5 × 24,5 m**. Quatro envelopes de colisores de caixa garantem paredes sólidas, preservando os acessos. Duas divisórias completas em `x = ±61,875` fecham o intervalo entre masculino e feminino.

Na terceira lista, os quatro conjuntos de cabines receberam cinco divisórias laterais e fechamentos frontais contínuos por banheiro. Dezesseis vãos livres de **1,15 m**, folhas de **1,14 × 2,59 m**, folga de **5 mm** por borda, batentes e puxadores. Componentes de abertura automática preservados com eixo de dobradiça corrigido para dentro. Geometria antiga das cabines retirada dos lotes exclusivos do Bloco 4; pias, divisórias dos mictórios e os 16 vasos já orientados para o interior preservados. Malhas derivadas editadas mantendo GUIDs e importadores; **assets de origem de LawSchool inalterados**. Comportamento e travessia não testados em jogo.

## Puzzle, painel e energia

Cinco pares preservados: quadrado=1, triângulo=2, círculo=3, losango=4 e cruz=5. Distribuição inicial atual: 402, 407, 414, 420 e **406**, respectivamente; os destinos podem mudar nos apagões, conforme o contrato abaixo. Ordem no quadro: triângulo, losango, quadrado, cruz, círculo. Senha **24153**; chave de acesso à biblioteca na bancada de alunos e espingarda na mesa docente da **423**, sem pedestal/suporte ou texto da arma. Sprite da chave reduzido; pickup, persistência e arma originais mantidos. Fotografias e definição do puzzle mantêm seus bytes/GUIDs. Os quatro monitores especiais acompanham a primeira estação das bancadas. Quadro personalizado, luminária, legenda e sequência de símbolos agora fixados na parede real do hall em `x = 7,5`, sem suporte isolado na circulação.

Símbolos e números aparecem **dentro das telas**, sem placas acima dos monitores. Os cinco pares ocupam a região inferior direita dos LCDs da primeira fileira, preservando as fotos e o terminal nos monitores originais. Geometrias e atlas originais reutilizados com três materiais de tela compartilhados, sem novos bitmaps. Placas azuis/presilhas inativas, fileIDs e valores preservados.

`Block4Keypad` conserva interação, pausa, teclado/controle, solução e restauração do controle. A interface agora representa uma caixa metálica com visor LCD, parafusos, indicadores e teclas. Erro: somente **“Senha incorreta”**, sem indicar onde procurar pistas. O estado variável do painel físico continua dinâmico; o HUD não foi aberto em execução.

`Block4PowerFluctuation` pertence somente a este mapa. Primeiro evento em **12–24 s**, próximos em **35–75 s**, pisca e apaga por **2–4 s**, mantendo iluminação residual pontual. Restaura intensidades e propriedades dos difusores, suspende a contagem nos menus e restaura ao desabilitar. Referências salvas no prefab; comportamento não executado em jogo.

`Block4PuzzleProgress` preserva porta e chave na sessão. A chave não reaparece depois da coleta. A espingarda segue o pickup global: o objeto do cenário pode reaparecer na recarga, enquanto a arma carregada pertence ao player persistente. Sem inventário paralelo ou gravação do puzzle em disco.

## Conexões

Chegada e `SpawnPoint`: **(0; 1,11; −44,55)**, ID `FromCampusExterior`. Saída: **(0; 1,4; −47,70)**, destino `CampusExterior / FromCampusBlock4`. Cena e contratos de transição preservados; exterior não alterado. Loading: `Resources/Textures/UI/Transitions/CampusBlock4.png` na pasta da cena.

## Validação desta revisão

Unity **6000.5.7f1**, URP do projeto, Windows/D3D12. Compilação e gravação via Unity API realizadas. Conferência por referências, matrizes, dimensões, colisores e previews em **1600 × 900**. Primeiras listas renderizadas em processo oculto/batch; terceira lista realizada **no editor já aberto**, em modo de edição, com prefab isolado e câmera fora de tela, **sem iniciar jogo ou Play Mode, abrir outro editor ou ativar/focar a janela existente**.

Previews atuais em `output/Bloco4-SegundaLista-*.png`: secretaria no hall/interior, janelas da 402, banheiros feminino/masculino, orientação do vaso, final oeste, quadro na parede, chave, espingarda, cinco pistas nas telas e planta. Capturas da primeira lista em `output/Bloco4-Correcoes-*.png` são históricas. Somente o teto é ocultado para o preview da planta.

Estado da terceira lista em `output/Bloco4-TerceiraLista-*.png`: entrada sem resquício, cabines, 424 como laboratório, 426/428 diante dos banheiros, depósito e planta atual. Capturas das listas anteriores registram os layouts anteriores; fotografias e pistas permanecem válidas. Nesta rodada foram mantidos todos os **37.492 IDs** existentes (**40.117 após acréscimos**), GUIDs/importadores, bytes da cena/scripts/fotos/puzzle e posições dos bancos/vasos do hall. Os **seis meshes novos** possuem referências persistentes; IDs locais e GUIDs externos resolvidos.

A disposição posterior dos bancos e vasos está em `output/Bloco4-Hall-Paredes-*.png` (central, entrada e retorno). Ajuste limitado às oito instâncias do hall, com todos os **37.492 IDs** do prefab preservados, sem alterar a cena, importadores existentes, modelos, materiais, scripts ou puzzle. Geometria de colisão considerada pelas suas transformações locais, pois os colisores do preview não recebem atualização de física. Prefab salvo/reaberto e folgas conferidas; sem Play Mode, jogo ou janela interativa.

Na segunda lista foram preservados os **35.352 registros existentes do prefab** (37.492 após os acréscimos), os **257 registros da cena**, os `.meta` anteriores, as fotografias/definição do puzzle e os assets de origem de LawSchool. IDs locais e GUIDs externos do prefab resolvidos; todos os novos meshes/materiais persistentes possuem consumidor. Scripts do painel, portas e evento de luzes foram alterados na primeira lista; seus bytes permaneceram intactos nesta segunda lista. Compilação e validação pela Unity API confirmaram cinco pistas, senha, chave/arma, 16 portas de cabine, quatro envelopes de banheiro, duas divisórias e desativação de 425/427. Pistas dimensionadas nos planos das telas e conferidas nos cinco previews dedicados.

**Não executados nesta revisão:** Play Mode, jogo, travessias/coletas em runtime, HUD em execução, evento de luzes em execução, build, benchmark de FPS, recálculo de NavMesh/oclusão e portais entre cenas. Os resultados de Play Mode documentados nas revisões antigas não validam este novo layout. Contagens de geometria não demonstram ganho de desempenho. Ferramentas temporárias de edição/preview retiradas após conferir resultados e dependências.

## Quarta lista — acesso e contratos compartilhados

Dois fechamentos antigos sobrepostos às frentes de 426/428 foram reduzidos a `x = 61,5…71,75`, mantendo a vedação além das salas e liberando portas/janelas. Gatilhos de aproximação: 2,35 × 3,2 × 3 m. Teste geométrico em Edit Mode com controle negativo da folha fechada e cápsula livre em cinco posições por porta com folha posada aberta; não testa a animação automática em jogo.

129 pisos receberam tag `Eco` e `FootstepSurface.Interior`. `Block4PowerFluctuation` tem primeiro evento em 12–24 s, intervalo de 35–75 s e apagão de 2–4 s no prefab, e agora notifica `Block4MonitorClues` ao iniciar o apagão completo. Chave original do puzzle conectada à cela 7 no inventário persistente existente; nova chave 8 na mesa docente da 428. Código 24153, pistas, fotos, arma e coleta original preservados. Assets compartilhados e dependências em [PrisonCellKeys.md](PrisonCellKeys.md).

Previews desta rodada: `output/Campus-Ultimas-Sala{426,428}-{Janela,VaoLivre}.png`, `Campus-Ultimas-Luzes-*.png` e `Campus-Ultimas-Chave-*.png`. Luzes e folhas foram posadas apenas para renderização fora de tela. Matrizes de chaves, duplicatas e IDs inválidos validados em objetos isolados no Editor. Cenas, importadores existentes, fotos/definição do puzzle preservados. Sem jogo, Play Mode, abertura/foco de janela, teste de gameplay, build ou FPS.

## Pistas que mudam de monitor — revisão anterior

`Block4MonitorClues`, salvo no prefab, referencia os cinco grupos originais de símbolo/dígito e 21 âncoras no `Monitor 24 polegadas` da `Estação 1`, na primeira fileira junto à janela. Apoio, salas inativas e a 423 trancada ficam fora dos destinos. Inicialmente: quadrado 402, triângulo 407, círculo 414, losango 420 e cruz **406**; a cruz saiu do laptop administrativo para obedecer à regra de primeira fileira. Nenhuma fotografia foi movida/editada.

Após intervalo de **180–300 s**, cada apagão completo oferece **25%** de chance de redistribuição. Todos os cinco pares mudam de monitor, sem destinos duplicados; a troca usa o evento `BlackoutStarted`, após as piscadas, com luz apagada. Nova espera de 180–300 s após sucesso. `Block4PuzzleProgress` conserva os destinos e o prazo na sessão; novo jogo limpa ambos. Após destravar a 423, as pistas param de mudar. Associações, código **24153**, quadro, fotos, pickups e importadores preservados. O campo editorial de localização em `Block4PuzzleDefinition` não dirige os destinos em runtime; o prefab/componente é a fonte atual das posições.

Os destinos são **salas aleatórias**, cada âncora em um laboratório diferente. A 426 aparece somente na demonstração de preview, sem privilégio ou destino fixo na lógica. Materiais originais das pistas preservados.

Checkpoint anterior à mecânica: **0fb6abbf**, enviado ao remoto antes desta implementação. Mil redistribuições isoladas, contrato das âncoras, duplicatas/destinos inválidos, compilação e referências conferidos em Edit Mode. Previews `output/Ultimos-Itens-Pista-*.png` e `Ultimos-Itens-Monitor-402-Apos-Troca.png`; a troca foi posada apenas para captura e não aciona o evento em jogo. Sem Play Mode, execução do jogo, abrir/focar janela, build ou FPS. O pedido de chave da manutenção/horda foi corrigido pelo usuário para **Direito**, sem acrescentar alarme ou trancar o depósito 419 do Bloco 4.

## Estado atual — 09/11/2026

As cinco pistas mudam para salas aleatórias distintas em cada apagão completo, sem a espera adicional de 180–300 s, sem chance de falhar e sem interromper as trocas após resolver o painel. Primeiro apagão em 8–15 s, próximos em 20–40 s. Mesmos 21 destinos na primeira fileira, mesmos pares e senha 24153.

Saída: materiais da fachada de entrada passam a dupla face por instâncias locais em runtime, preservando os assets compartilhados. Near clip da câmera limitado a 0,06 m enquanto essa saída estiver carregada, com restauração ao destruir o objeto na troca de cena. Isso evita corte próximo e desaparecimento por face invertida. Porta, colisores e CampusExterior / FromCampusBlock4 mantidos. Sem Unity/Play Mode, build ou capturas nesta alteração; o usuário fará a validação.

## Energia e saída — atualização de 09/11/2026

Primeira falha em 6–10 s, próximas em 12–22 s; apagão de 2–4 s preservado. Cada apagão completo redistribui os cinco pares símbolo/número conforme as regras existentes. A saída recebe fechamento visual atrás do gatilho para ocultar o vazio, sem colisão extra ou alteração do destino. Sem Unity, jogo, testes, previews, capturas ou vídeos.

Verificação do acesso de 09/11: cena e prefab reabertos, uma recompensa de acesso à biblioteca, zero chaves de cela ativas, referências da porta/painel e trigger preservadas. Sprite azul novo e texto de inventário atualizados. Conferência de edição; sem coleta real, senha por input ou novo Play Mode. [Resultados do acesso](CampusLibrary.md#verificação-do-acesso--09112026).
