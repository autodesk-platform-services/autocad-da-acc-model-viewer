import { folderDetails } from './sidebar.js';

function showNotification(message, nodeId) {
    // Remove any existing notifications
    document.querySelectorAll('.bubble-notification').forEach(notification => notification.remove());

    // Create a new notification
    const bubble = document.createElement('div');
    bubble.className = 'bubble-notification';
    bubble.textContent = message;

    // Find the DOM element of the tree node and append the bubble
    const nodeElement = document.querySelector(`[data-uid="${nodeId}"]`);
    if (nodeElement) {
        nodeElement.appendChild(bubble);

        // Position the bubble
        const rect = nodeElement.getBoundingClientRect();
        bubble.style.top = `${rect.top - 30}px`;
        bubble.style.left = `${rect.left}px`;

        // Remove the bubble after a few seconds
        setTimeout(() => bubble.remove(), 3000);
    } else {
        console.error(`Node element with ID ${nodeId} not found.`);
    }
}

export async function startConnection(onReady) {
    if (connection && connection.connectionState) {
        return connectionId;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/api/signalr/designautomation")
        .build();

    connection.on("onProgress", (message) => {
        writeLog(message);
    });

    connection.on("onSuccess", (message) => {
        const folderInfo = folderDetails.getFolderInfo();
        if (folderInfo && folderInfo.nodeId) {
            showNotification("Collaboration File Created!!", folderInfo.nodeId);
        } else {
            console.error("Folder info or nodeId not available.");
        }
    });

    connection.on("onComplete", (message) => {
        writeLog(message);
    });

    try {
        await connection.start();
        connectionId = await connection.invoke("getConnectionId");
        writeLog("Connection started: " + connectionId);
        if (onReady) onReady();
        return connectionId;
    } catch (error) {
        console.error("Error starting connection:", error);
    }
}

export const startWorkitem = async () => {
    try {
        const browserConnectionId = await startConnection();
        const formData = new FormData();
        const folderInfo = folderDetails.getFolderInfo();
        if (folderInfo) {
            const { hubId, projectId, folderId, nodeId } = folderInfo;
            formData.append("data", JSON.stringify({
                hubId,
                projectId,
                folderId,
                browserConnectionId
            }));

            writeLog("Sending selected folder to DA server to place the Collaboration item");

            const response = await fetch("api/da/workitems", {
                method: "POST",
                body: formData,
            });

            if (response.ok) {
                const data = await response.json();
                writeLog("Workitem started: " + data.workItemId);
            } else {
                console.error("Error in response from server:", response.statusText);
            }
        } else {
            console.error("Folder info not available.");
        }
    } catch (error) {
        console.error("Error sending workitem:", error);
    }
};

export const writeLog = (text) => {
    const logEntry = document.createElement("div");
    logEntry.classList.add("log-entry");
    logEntry.textContent = text;

    const outputLog = document.getElementById("outputlog");
    if (outputLog) {
        outputLog.appendChild(logEntry);
        outputLog.scrollTop = outputLog.scrollHeight;
    } else {
        console.error("Output log element not found.");
    }
};

let connection;
let connectionId;
