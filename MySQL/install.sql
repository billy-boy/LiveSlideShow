
CREATE DATABASE IF NOT EXISTS `slideshow` /*!40100 DEFAULT CHARACTER SET utf8 */$$

USE `slideshow`$$

CREATE TABLE IF NOT EXISTS `tblimages` (
  `imageID` int(11) NOT NULL AUTO_INCREMENT,
  `imagePath` varchar(255) NOT NULL,
  `importDate` datetime DEFAULT NULL,
  `viewDate` datetime DEFAULT NULL,
  `uploadDate` datetime DEFAULT NULL,
  `viewCount` int(11) DEFAULT '0',
  PRIMARY KEY (`imageID`),
  KEY `indViewed` (`viewDate`)
) ENGINE=MyISAM AUTO_INCREMENT=1 DEFAULT CHARSET=utf8$$
