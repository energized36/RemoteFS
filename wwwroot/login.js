class LoginHandler {
    constructor(){
        this.loginButton = document.getElementById("login-btn");
        this.loginButton.addEventListener("click", () => this.login())
    }

    async login(){
        const username = document.getElementById("user").value;
        const password = document.getElementById("pw").value;
        const response = await fetch("http://localhost:5247/api/auth/login", {
            method: "POST",
            credentials: "include",
            headers: {"Content-Type": "application/json"},
            body: JSON.stringify({username, password})
        });
        if (response.ok) {
            window.location.href = "/"
        }
        else {
            alert("something went wrong");
        }
    }
    
}

const loginHandler = new LoginHandler();