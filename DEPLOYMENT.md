# Flight Optimizer Deployment Guide

This guide will help you deploy your full-stack application for free using **Vercel** (Frontend), **Render** (Backend), and **Neon** (Database).

## Prerequisites
- [GitHub Account](https://github.com/)
- [Vercel Account](https://vercel.com/)
- [Render Account](https://render.com/)
- [Neon Account](https://neon.tech/)

---

## 1. Database Setup (Neon.tech)
1.  Log in to [Neon Console](https://console.neon.tech/).
2.  Create a new project named `flight-optimizer`.
3.  **Copy the Connection String.** It will look like: 
    `postgres://neondb_owner:*******@ep-cool-frog-123456.us-east-2.aws.neon.tech/neondb?sslmode=require`
4.  **Important:** You do *not* need to run any SQL scripts. The application will automatically create the database schema and seed data when it starts.

---

## 2. Backend Deployment (Render)
1.  Log in to [Render Dashboard](https://dashboard.render.com/).
2.  Click **New +** -> **Web Service**.
3.  Connect your GitHub repository `flight-optimizer`.
4.  **Configure the Service:**
    - **Name:** `flight-optimizer-api`
    - **Runtime:** `Docker` (Render will auto-detect the Dockerfile).
    - **Region:** Choose one close to you.
    - **Free Instance Type:** Select the Free tier.
5.  **Environment Variables:**
    Add the following variables:
    - `DbProvider`: `PostgreSQL`
    - `DefaultConnection`: *(Paste your Neon Connection String from Step 1)*
    - `ASPNETCORE_ENVIRONMENT`: `Production`
6.  Click **Create Web Service**.
7.  **Wait for Deployment.** It may take a few minutes.
8.  **Copy the Backend URL.** Once live, see the URL at the top (e.g., `https://flight-optimizer-api.onrender.com`).

---

## 3. Frontend Deployment (Vercel)
1.  Log in to [Vercel Dashboard](https://vercel.com/dashboard).
2.  Click **Add New...** -> **Project**.
3.  Import your `flight-optimizer` repository.
4.  **Configure Project:**
    - **Framework Preset:** Angular (should be auto-detected).
    - **Root Directory:** `frontend` (Click Edit to change from root to `frontend`).
5.  **Environment Variables:**
    - `ANGULAR_API_URL`: *(Paste your Render Backend URL from Step 2)*.
    *Note: You may need to update `src/environments/environment.prod.ts` to use this variable or verify your code uses it.*
6.  Click **Deploy**.

---

## Testing
1.  Open your Vercel URL.
2.  The application should load.
3.  The backend might take a minute to "wake up" on the free tier (cold start).
