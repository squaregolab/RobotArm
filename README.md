##**RobotArm - Drive a robot arm whith Kinect**
Code *Xaml* & *C#* pour contrôler le bras robotique avec un Kinect (xbox 360) et bracelet Myo.


Le Kinect est relié au PC. Les données récupérés pour le contrôle du bras sont les articulations.

Les articulations permettent le calcul des angles réalisé avec le bras. Le Kinect ne pouvant pas détecter les mouvements de la main, nous utilisons un Myo.

Le Myo permet de détecter les mouvements de la main. Le logiciel utilisé est celui fourni avec, permettant de mapper un mouvement sur une touche de clavier.

Le bras robotique est contrôlé par une carte électonique faites pour le contrôle de servomoteurs. Cette carte est la *SSC-32U*.

La communication entre la carte et le PC est série via USB. La trame envoyée à la carte suit la doc présent dans le dossier _docs_.


Ce projet a été réalisé pour le _Mini Maker Faire 2017_ de Perpignan.

Par _Florentin DION_ et _Guillaume DUPLOUICH_. :shipit: