class Client {
    constructor(){
        this.currentPath = "";
        const wsProtocol = location.protocol === "https:" ? "wss" : "ws";
        this.webSocket = new WebSocket(`${wsProtocol}://${location.host}/ws/watch`);
        this.setupListener();
    }

    // set up websocket listener
    setupListener(){
        this.webSocket.onmessage = ({ data }) => {
            const event = JSON.parse(data);
            switch (event.type) {
                case 'created': this.onFileCreated(event); break;
                case 'deleted': this.onFileDeleted(event); break;
                case 'renamed': this.onFileRenamed(event); break;
                case 'changed': this.onFileChanged(event); break;
            }
        }
        this.webSocket.onclose = () => {
            console.log('Watcher disconnected, reconnecting...');
            setTimeout(() => this.reconnect(), 3000); // retry after 3s
        };
        this.webSocket.onerror = () => this.webSocket.close(); // trigger onclose for retry
    }

    // websocket functions
    reconnect(){
        const wsProtocol = location.protocol === "https:" ? "wss" : "ws";
        this.webSocket = new WebSocket(`${wsProtocol}://${location.host}/ws/watch`);
        this.setupListener();
    }

    // file event handler functions
    onFileCreated(event) {
        if (this.getParentPath(event.path) !== this.currentPath) return;
        this.renderFiles(this.currentPath, false);
    }

    onFileDeleted(event) {
        if (this.getParentPath(event.path) !== this.currentPath) return;
        this.renderFiles(this.currentPath, false);
    }

    onFileRenamed(event) {
        if (this.getParentPath(event.path) !== this.currentPath) return;
        this.renderFiles(this.currentPath, false);
    }

    onFileChanged(event) {
        // Optionally highlight the file briefly to indicate it was modified
        const el = document.querySelector(`[data-name="${event.name}"]`);
        // ToDo: add logic to do something here
    }

    getParentPath(path) {
        const parts = path.split('/');
        parts.pop();
        return parts.join('/');
    }

    getFileName(path) {
        return path.split('/').pop();
    }

    isVideo(name){
        return [".mp4", ".webm", ".ogg", ".mov"].includes(name.slice(name.lastIndexOf('.')).toLowerCase());
    }

    openVideo(url){
        const player = document.getElementById("video-player");
        const overlay = document.getElementById("video-overlay");
        player.src = url;
        overlay.classList.add("active");
        player.play();
    }

    formatSize(bytes){
        if (bytes == null) return "--";
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 ** 2) return `${(bytes / 1024).toFixed(1)} KB`;
        if (bytes < 1024 ** 3) return `${(bytes / 1024 ** 2).toFixed(1)} MB`;
        return `${(bytes / 1024 ** 3).toFixed(1)} GB`;
    }

    // api calls
    async logout(){
        const response = await fetch("/api/auth/logout", {
            method: "POST",
            credentials: "include"
        });
        if (response.ok) {
            window.location.href = "/login.html";
        }
    }
    
    deleteFile(path, element){
        fetch(`/api/files?path=${encodeURIComponent(path)}`, { method: "DELETE" })
            .then(() => element.remove());
    }
    
    getFiles(path){
        return fetch(`/api/files/list?path=${encodeURIComponent(path)}`).then(res => res.json());
    }

    
    updateBreadcrumbs(path){
        const breadcrumbs = document.getElementById("breadcrumbs");
        breadcrumbs.innerHTML = "";

        const root = document.createElement("span");
        root.innerText = "Home";
        root.classList.add("crumb");
        root.addEventListener("click", () => this.renderFiles(""));
        breadcrumbs.appendChild(root);

        if (path === "") return;

        const parts = path.split("/").filter(p => p);
        parts.forEach((part, i) => {
            const sep = document.createElement("span");
            sep.innerText = " > ";
            breadcrumbs.appendChild(sep);

            const crumb = document.createElement("span");
            crumb.innerText = part;
            crumb.classList.add("crumb");
            const crumbPath = "/" + parts.slice(0, i + 1).join("/");
            crumb.addEventListener("click", () => this.renderFiles(crumbPath));
            breadcrumbs.appendChild(crumb);
        });
    }

    async renderFiles(path = "", pushState = true){
        let filesContainer = document.getElementById("files-container");
        let directorySidebar = document.getElementById("directory-sidebar");
        let files = await this.getFiles(path);
        this.currentPath = path;
        this.updateBreadcrumbs(path);

        if (pushState) {
            history.pushState({ path }, "", `?path=${encodeURIComponent(path)}`);
        }
        document.getElementById("back-btn").disabled = path === "";
        filesContainer.innerHTML = "";
        directorySidebar.innerHTML = "";
        files.forEach(file => {
            const fileElement = document.createElement("div");
            const nameDiv = document.createElement("div");
            nameDiv.innerText = file.name;
            fileElement.appendChild(nameDiv);
            if (file.isDirectory){
                fileElement.classList.add("folder-card");
                fileElement.addEventListener("click", () => this.renderFiles(`${path}/${file.name}`));
                directorySidebar.appendChild(fileElement);
            } else {
                fileElement.classList.add("file-card");
                if (this.isVideo(file.name)) {
                    fileElement.addEventListener("click", () => this.openVideo(
                        `/api/files/stream?path=${encodeURIComponent(`${path}/${file.name}`)}`
                    ));
                }

                const rightDiv = document.createElement("div");
                rightDiv.classList.add("file-card-right");

                const dateDiv = document.createElement("div");
                dateDiv.innerText = new Date(file.modified).toLocaleString();
                const sizeDiv = document.createElement("div");
                sizeDiv.innerText = this.formatSize(file.size);
                const downloadBtn = document.createElement("button");
                downloadBtn.innerText = "Download";
                downloadBtn.classList.add("download-btn");
                downloadBtn.addEventListener("click", (e) => {
                    e.stopPropagation();
                    const a = document.createElement("a");
                    a.href = `/api/files/download?path=${encodeURIComponent(`${path}/${file.name}`)}`;
                    a.download = file.name;
                    a.click();
                });

                const deleteBtn = document.createElement("button");
                deleteBtn.innerText = "Delete";
                deleteBtn.classList.add("delete-btn");
                deleteBtn.addEventListener("click", (e) => {
                    e.stopPropagation();
                    this.deleteFile(`${path}/${file.name}`, fileElement);
                });

                rightDiv.appendChild(sizeDiv);
                rightDiv.appendChild(dateDiv);
                rightDiv.appendChild(downloadBtn);
                rightDiv.appendChild(deleteBtn);
                fileElement.appendChild(rightDiv);
                filesContainer.appendChild(fileElement);
            }
        })
    }
    
    start(){
        console.log("client started");
        const initialPath = new URLSearchParams(location.search).get("path") ?? "";
        this.renderFiles(initialPath, false);
        history.replaceState({ path: initialPath }, "", `?path=${encodeURIComponent(initialPath)}`);

        document.getElementById("logout-btn").addEventListener("click", (e) => {
            this.logout();    
        })
        
        document.getElementById("back-btn").addEventListener("click", () => {
            this.renderFiles(this.getParentPath(this.currentPath));
        });

        document.getElementById("video-overlay").addEventListener("click", (e) => {
            if (e.target === e.currentTarget) {
                const player = document.getElementById("video-player");
                player.pause();
                player.src = "";
                e.currentTarget.classList.remove("active");
            }
        });

        window.addEventListener("popstate", (e) => {
            this.renderFiles(e.state?.path ?? "", false);
        });

        const input = document.getElementById("upload-input");
        const progressBar = document.getElementById("upload-progress");
        const progressFill = document.getElementById("upload-progress-bar");
        const progressLabel = document.getElementById("upload-progress-label");

        input.addEventListener("change", () => {
            if (!input.files[0]) return;
            const formData = new FormData();
            formData.append("file", input.files[0]);

            const xhr = new XMLHttpRequest();

            xhr.upload.addEventListener("progress", (e) => {
                if (!e.lengthComputable) return;
                const pct = Math.round((e.loaded / e.total) * 100);
                progressBar.classList.remove("hidden");
                progressFill.style.width = `${pct}%`;
                progressLabel.textContent = `Uploading... ${pct}%`;
            });

            xhr.addEventListener("load", () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    progressFill.style.width = "100%";
                    progressLabel.textContent = "Upload complete!";
                    progressFill.classList.add("success");
                    setTimeout(() => {
                        progressBar.classList.add("hidden");
                        progressFill.style.width = "0%";
                        progressFill.classList.remove("success");
                    }, 2000);
                    this.renderFiles(this.currentPath, false);
                } else {
                    progressLabel.textContent = "Upload failed.";
                    progressFill.classList.add("error");
                    setTimeout(() => {
                        progressBar.classList.add("hidden");
                        progressFill.style.width = "0%";
                        progressFill.classList.remove("error");
                    }, 3000);
                }
                input.value = "";
            });

            xhr.addEventListener("error", () => {
                progressLabel.textContent = "Upload failed.";
                progressFill.classList.add("error");
                setTimeout(() => {
                    progressBar.classList.add("hidden");
                    progressFill.style.width = "0%";
                    progressFill.classList.remove("error");
                }, 3000);
                input.value = "";
            });

            xhr.open("POST", `/api/files/upload?path=${encodeURIComponent(this.currentPath)}`);
            xhr.withCredentials = true;
            xhr.send(formData);
        });
    }
}

let c = new Client();
c.start();