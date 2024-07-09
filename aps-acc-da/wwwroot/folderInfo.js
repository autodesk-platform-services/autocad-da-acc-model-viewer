export class FolderInfoManager {
    constructor() {
        this.folderInfo = null;
    }
    setFolderInfo(node) {
        const tokens = node.id.split('|');
        this.folderInfo = {
            hubId: tokens[1],
            projectId: tokens[2],
            folderId: tokens[3],
            nodeId: node.id

        }
    }
    getFolderInfo() {
        return this.folderInfo;
    }

}
