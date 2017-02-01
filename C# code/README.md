#**Programmation C# - Visual Studio**
##Explication du code

######*Kinect*
>Le squelette 
Le Kinect détecte les articulations de l'utilisateur et les dessines à l'écran.
Il relie ensuite chaque articulations entre elles pour réaliser le squelette.

Nous pouvons récupérer à partir de chaque articulations leurs coordonnées {x ; y ; z}.

:warning:Les valeurs prisent sont comprisent entre -1 et 1. Les coordonnées {0 ; 0 ; 0} sont le centre de l'image.

>Les angles
Pour calculer les angles, nous utilisons l'une des formules d'Al Kashi, [*la loi des Cosinus*](http://villemin.gerard.free.fr/GeomLAV/Triangle/Calcul/RelQuel.htm#cosinus).

Les angles du poignet, coude et épaule utilise les coordonnées {x ; y}, et l'angle de la hanche {x ; z}.
Les angles calculés sont ensuite à convertir de radian à degrées, c'est à dire : `degrées = radian * 180 / PI` 

######*Myo*
>Mouvements
Le bracelet détecte les mouvements du muscle de l'avant-bras. Il permet de détecter 5 mouvements différents :
- Ouvrir la main
- Fermer la main
- Coup de poignet de gauche à droite
- Coup de poignet de droite à gauche 
- Double "tap" des doigts

>Gestion du mouvement dans le code
Le bracelet détectant plus ou moins bien les 2 premiers mouvements cités, nous avons opté sur le double "tap" pour ouvrir et fermer la main.
Pour gérer ce mouvement dans le code, nous utilisons le logiciel de Myo et mappons une touche du clavier sur ce mouvement.
A chaque fois que le mouvement est réalisé, la touche est simulé à l'ordinateur. La touche pressé est donc détecté par le programme qui va envoyé l'information au bras d'ouvrir ou femrer sa main.

:warning:Le bracelet peut être trop gros pour l'avant-bras d'enfants.


######*SSC-32U*
>UART
Le bras robotique est contrôlé par une carte électonique faites pour le contrôle de servomoteurs. Cette carte est la *SSC-32U*.
La communication entre la carte et le PC est série via USB. Plus précisemment :
- Baud Rate : 9600 (possibilité de le modifier)
- Data bits : 8
- Hand Shaking : None
- Parité : None
- Bits de stop : 1

>Trame
La carte contrôle les servomoteurs en fonction des trames reçues. La trame à envoyé suit le schéma suivant :
`#[XX] P[YYYY] T[ZZZZ]`

[XX]   : est le numéro du connectique du servomoteur à contrôler.

[YYYY] : est la position du servomoteur (compris entre 500 et 2500). 

[ZZZZ] : est le temps que met le servomoteur pour atteindre sa position (en milliseconde).


Le schéma peut être répété pour chaque servomoteur en une seule trame. 

Exemple : `#[10] P[1500] T[1000] #[5] P[756] T[750] #[13] P[2320] T[500]`

Il est possible d'imposé un même temps pour tous les servomoteurs en ne mettant que le T[ZZZZ] en toute fin de trame. 

Exemple : `#[10] P[1500] #[5] P[756] #[13] P[2320] T[1000]`

Plus d'information dans le document à ce sujet dans le dossier docs de ce GitHub.