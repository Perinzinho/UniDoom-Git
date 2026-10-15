# Cylinder — cantina e portas sem textos

Revisão de 15/10/2026, Unity 6000.5.7f1 / URP 17.5.

O prefab conserva a estrutura circular, a cantina central, o mobiliário e os
seis pontos de spawn. Os textos TextMesh são esvaziados e seus renderizadores
continuam desabilitados. Letras feitas em meshes são ocultadas. O acabamento
dos três quadros elétricos importados passa a usar metal PBR sem inscrições,
removendo também as palavras e os números que estavam na textura original.

As portas recebem plaquinhas físicas com pictogramas em geometria, sem palavras
nem números. A orientação é a de quem entra pelo sul e olha para a cafeteria:

| Posição | Destino | Identidade visual |
|---|---|---|
| Direita, bay 12, +X | Sala de aula | Quadro/carteira em placa verde e visor institucional |
| Esquerda, bay 36, −X | Biblioteca | Livro aberto e acabamento de madeira |
| Atrás da cafeteria, bay 24, −Z | Diretoria | Poltrona executiva com louros e detalhe em latão |
| Em frente à cafeteria, bay 0, +Z | Entrada principal | Passagem/seta e folha reforçada maior |

As outras 43 portas decorativas conservam o bloqueio e recebem símbolos de
elétrica, depósito, manutenção ou sanitário. Há 47 plaquinhas ao todo e quatro
portas funcionais. A entrada principal mede **2,60 × 3,45 m**; as outras três
folhas funcionais mantêm **1,85 × 3,25 m**. O vão secundário adjacente é fechado
com alvenaria para direcionar a circulação pela nova entrada. A nova folha usa
`OpenDoorWithKey`, abre por aproximação e não exige chaves.

Os modelos novos da cantina incluem 16 cafeteiras com grupos de extração,
manômetro sem números, botões, vaporizador e bandeja vazada; 16 urnas com perfil
curvo, tampa, alças e torneira; 48 xícaras ocas com alças; armários com painéis,
puxadores, bancada com borda arredondada, fixações, cobertura com bordas enroladas
e exaustor modelado. Os marcadores e colisores de apoio originais são mantidos.
O cardápio e a lona que cobriam as máquinas ficam ocultos, preservando seus assets.

A geometria é autoral e persistente em `Cylinder/Meshes/CanteenDoors/`.
Os materiais novos ficam em `Cylinder/Materials/CanteenDoors/`. Texturas e shader
PBR existentes são reutilizados; nenhum novo download ou alteração de shader foi
necessário. As peças são agrupadas por acabamento em quatro setores da cantina
e oito setores de plaquinhas. Objetos temporários de modelagem são removidos
somente da nova hierarquia antes de salvá-la, sem excluir assets existentes.

## Evidências

- [Cantina](Canteen.png) e [vista mais ampla](CanteenWide.png).
- [Sala de aula](ClassroomDoor.png), [biblioteca](LibraryDoor.png),
  [diretoria](DirectorDoor.png) e [entrada maior](FrontDoor.png).
- [Editor](edit-validation.txt), [Play Mode](play-validation.txt),
  [preservação dos IDs](structure-validation.json) e [integração](installed.json).

As capturas usam a atmosfera e a lanterna existentes no prefab. As contagens
em [asset-counts.txt](asset-counts.txt) não são uma medição de desempenho.
Build Windows, profiling em hardware-alvo, combate completo e playtest da cena
agregadora não fazem parte das verificações executadas nesta revisão.

O log de Play Mode inclui a exceção interna de inicialização
`UnityEditor.Search.SearchDatabase` já observada nesta cópia isolada na revisão
anterior. Ela não impediu o teste físico nem o resultado
`CYLINDER_CANTEEN_PLAY_PASS`; não é apresentada como erro do prefab ou do projeto
principal.

## Reprodução e reversão

`prepare.py` guarda o prefab recebido e os hashes em
`.utmp/CylinderCanteenDoorsBefore`, e sincroniza a cópia isolada existente.
O helper de geometria reutiliza os algoritmos da revisão anterior, com pasta
de saída separada. Copiar os três arquivos C# desta pasta para
`Assets/_TemporaryTools/Editor` da cópia isolada.

1. Executar `CylinderCanteenBuilder.Build` na Unity isolada.
2. Executar `CylinderCanteenVerifier.PrepareFinal` para capturas e conferência.
3. `py Tools/Cylinder/CanteenDoors/install.py` prepara a integração do YAML.
4. Executar `CylinderCanteenVerifier.Verify` e `RunPlayMode`. O último encerra
   o Editor ao terminar e precisa ser iniciado sem `-quit`.
5. `install.py --install` exige ambos os PASS e recusa alterações concorrentes.

A integração conserva todos os IDs recebidos e todos os blocos originais de
scripts de gameplay, colisores e MeshFilters. Nos objetos antigos, só entram as
alterações de renderização, remoção de texto e ligações a filhos novos.
Não há modificações intencionais em outros cenários, scripts runtime ou materiais
originais compartilhados.

`py Tools/Cylinder/CanteenDoors/install.py --restore-prefab` restaura o prefab
recebido se seu hash ainda coincidir com o resultado instalado. Edições posteriores
não são sobrescritas. As adições permanecem disponíveis; nenhum arquivo é apagado.
