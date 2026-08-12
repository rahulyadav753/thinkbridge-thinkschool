import { PublicClientApplication } from "@azure/msal-browser";

const msalConfig = {
    auth: {
        clientId: "5b580829-3333-4b8b-8b9b-197a35bd28e",
        authority:
            "https://login.microsoftonline.com/b69d82df-4ebe-474d-9ac7-00efbf13427e",
        redirectUri: "http://localhost:5173"
    }
};

const msalInstance = new PublicClientApplication(msalConfig);

const loginRequest = {
    scopes: [
        "api://88d2e9d0-3cef-4f68-bb6c-0a7512c89fea/access_as_user"
    ]
};

const loginButton = document.getElementById("loginButton");
const logoutButton = document.getElementById("logoutButton");
const quotesButton = document.getElementById("quotesButton");
const status = document.getElementById("status");
const output = document.getElementById("output");

// ============================================================
// Initialize MSAL
// ============================================================

async function initializeMsal() {
    try {
        // Initialize MSAL first
        await msalInstance.initialize();

        // Handle redirect response from Microsoft
        const response =
            await msalInstance.handleRedirectPromise();

        if (response) {
            msalInstance.setActiveAccount(response.account);

            status.textContent =
                `Signed in as ${response.account.username}`;

            loginButton.style.display = "none";
            logoutButton.style.display = "inline-block";
            quotesButton.style.display = "inline-block";

            output.textContent = "Login successful.";

            return;
        }

        // Check if an account is already signed in
        const accounts =
            msalInstance.getAllAccounts();

        if (accounts.length > 0) {
            msalInstance.setActiveAccount(accounts[0]);

            status.textContent =
                `Signed in as ${accounts[0].username}`;

            loginButton.style.display = "none";
            logoutButton.style.display = "inline-block";
            quotesButton.style.display = "inline-block";
        }
    }
    catch (error) {
        console.error("MSAL initialization error:", error);

        output.textContent =
            `MSAL initialization failed:\n${error.message}`;
    }
}

// ============================================================
// Login
// ============================================================

loginButton.addEventListener("click", async () => {
    try {
        await msalInstance.loginRedirect(loginRequest);
    }
    catch (error) {
        console.error("Login error:", error);

        output.textContent =
            `${error.errorCode || "Error"}: ${error.message}`;
    }
});

// ============================================================
// Get Entra token and call Quotes API
// ============================================================

quotesButton.addEventListener("click", async () => {
    try {
        const account =
            msalInstance.getActiveAccount();

        if (!account) {
            throw new Error("No signed-in account.");
        }

        // Get access token silently
        const tokenResponse =
            await msalInstance.acquireTokenSilent({
                ...loginRequest,
                account: account
            });

        const accessToken =
            tokenResponse.accessToken;

        console.log(
            "Entra access token:",
            accessToken
        );

        // Call Quotes API
        const response = await fetch(
            "http://localhost:5228/api/quotes",
            {
                method: "GET",
                headers: {
                    Authorization:
                        `Bearer ${accessToken}`
                }
            }
        );

        const result =
            await response.text();

        output.textContent =
            `Status: ${response.status}\n\n${result}`;
    }
    catch (error) {
        console.error(
            "Quotes API error:",
            error
        );

        output.textContent =
            `${error.errorCode || "Error"}: ${error.message}`;
    }
});

// ============================================================
// Logout
// ============================================================

logoutButton.addEventListener("click", async () => {
    try {
        await msalInstance.logoutRedirect();
    }
    catch (error) {
        console.error(
            "Logout error:",
            error
        );

        output.textContent =
            `${error.errorCode || "Error"}: ${error.message}`;
    }
});

// ============================================================
// Start MSAL
// ============================================================

initializeMsal();