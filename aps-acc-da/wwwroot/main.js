import { initViewer, loadModel } from './viewer.js';
import { initTree } from './sidebar.js';
import { startWorkitem } from './apsda.js';

const login = document.getElementById('login');
const submit = document.getElementById('submit');
const log = document.getElementById('log');
const copy = document.getElementById('copyButton');



async function handleLogin() {
    try {
        const response = await fetch('/api/auth/profile');
        if (response.ok) {
            const user = await response.json();
            initializeUserSession(user);
        } else {
            initializeGuestSession();
        }
    } catch (err) {
        alert('Could not initialize the application. See console (F12) for more details.');
        console.error(err);
    }
}

function initializeUserSession(user) {
    login.innerText = `Logout (${user.name})`;
    submit.style.visibility = 'visible';
    log.style.visibility = 'visible';
    submit.onclick = startWorkitem;
    login.onclick = handleLogout;
    copy.onclick = copyLog;
    initializeViewerAndTree();
}

function initializeGuestSession() {
    login.innerText = 'Login';
    login.onclick = () => {
        window.location.replace('/api/auth/login');
    };
    login.style.visibility = 'visible';
}

async function copyLog() {
    // Get the content of the pre element
    const text = document.getElementById("outputlog").innerText;

    if (navigator.clipboard) {
        // Use the modern clipboard API
        try {
            await navigator.clipboard.writeText(text);
            alert('Copied to clipboard!');
        } catch (err) {
            console.error('Failed to copy: ', err);
        }
    } else {
        // Throw an error if the clipboard API is not supported
        alert('Clipboard API not supported. Please upgrade to the latest version of your browser.');
        throw new Error('Clipboard API not supported. Please upgrade to the latest version of your browser.');
    }
}

function handleLogout() {
    const iframe = document.createElement('iframe');
    iframe.style.visibility = 'hidden';
    iframe.src = 'https://accounts.autodesk.com/Authentication/LogOut';
    document.body.appendChild(iframe);
    iframe.onload = () => {
        window.location.replace('/api/auth/logout');
        document.body.removeChild(iframe);
    };
}

async function initializeViewerAndTree() {
    const viewer = await initViewer(document.getElementById('preview'));
    initTree('#tree', id => loadModel(viewer, window.btoa(id).replace(/=/g, '')));
}

handleLogin();
