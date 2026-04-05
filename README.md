# The Watch Vault

- Enmanuel De los Santos
- Braulio Garcia Gonzalez
- Mark Anthony Calla Vargaya
- Aiden Barrett
- Roberto Sanchez Molina

# Project Proposal

- __Title__: The Watch Vault

- __Overview__: The goal of this project is to build a professional eCommerce website for browsing through a collection of watches. The purpose is to give each member of this organization experience with technologies related to the projects development.

- __Scope__: This project will create a place where you could come to browse watches of different brands and types. The application features a fully reactive front-end, Google OAuth integration, interactive search, active Shopping Cart tracking, and a dynamic Inventory system.

- __Functional Requirements__:
    - Pages:
        - Landing page for navigation and site introduction
        - Shop (Dynamic Watch Grid with filter/search parameters)
        - Profile Center
        - Shopping Cart
        - Login / Register 
    - Database Strategy:
        - Google Cloud Firestore implementation instead of DynamoDB for user account and watch data orchestration.

# Getting Started (Developer Guide)
This is a standard .NET 9 Blazor Application leveraging Interactive Server logic and static routing.

## Setup Requirements
1. **.NET 9 SDK**: Ensure your environment has the latest SDK installed.
2. **Google Cloud Firestore**: You must provide the `firebase-credentials-the-watchvault.json` secret within the Root Directory for backend connections to initialize.
3. **Restoring Packages**: 
```bash
dotnet restore
```

## Running the Application
Use the standard .NET CLI to execute the development server. The `dotnet watch` command enables Hot Reloading for immediate visualization of component changes.
```bash
dotnet watch
```
Navigate to `https://localhost:7055` (or whichever port is defined in your launchSettings).

## Usage Flow
1. **Browse as Guest**: Visitors can view the Landing Page and Register.
2. **Authentication**: Sign up natively or use **Google OAuth** to automatically create a Vault Profile.
3. **Shopping**: Head to the `/shop` route to view live Inventory downloaded directly from Firestore. 
4. **CRUD Actions**: The `Add to Cart` functionality creates secure Database Subcollections under your user profile. The `/cart` component allows reading, updating quantities, and deleting items natively against the database. Checking out destructively alters the global Watch Inventory quantities, fulfilling comprehensive structural specifications.
