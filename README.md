# Generating Collaboration Files in AutoCAD with Design Automation for ACC Viewing

Using Design Automation for AutoCAD, you can generate a collaboration file containing various viewable assets. This file, recognizable by any LMV-based application, can be hosted on Autodesk Construction Cloud for model viewing.

The Model Derivative API translates over 60 file formats into derivatives (output files), including the collaboration format. While the AutoCAD API directly generates collaboration files from 3D drawings, the Design Automation service's API offers greater flexibility. It allows you to not only generate these files but also manipulate the properties of the 3D model within them.

This project focuses on removing generic properties associated with the 3D model. We retain only basic Model objects with identifying properties like Name and Handle ID.

Handles are unique identifiers within a single AutoCAD DWG database. They are 64-bit integers introduced before AutoCAD R13 and persist across sessions. However, handles are not unique across different databases. Since all databases start with the same initial handle value, duplication is almost guaranteed.

Refer [Supported Translation](https://aps.autodesk.com/en/docs/model-derivative/v2/developers_guide/supported-translations/) document.
| COLLABORATION | SVF<br>SVF2<br>Thumbnail |
| :------------- | :------------------------------ |
