# React + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Babel](https://babeljs.io/) (or [oxc](https://oxc.rs) when used in [rolldown-vite](https://vite.dev/guide/rolldown)) for Fast Refresh
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/) for Fast Refresh

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend using TypeScript with type-aware lint rules enabled. Check out the [TS template](https://github.com/vitejs/vite/tree/main/packages/create-vite/template-react-ts) for information on how to integrate TypeScript and [`typescript-eslint`](https://typescript-eslint.io) in your project.

```bash
az ad sp create-for-rbac \
  --name "fab4kids-github-actions" \
  --role contributor \
  --scopes /subscriptions/944afe5b-566b-4749-80cd-5c516348ca7a/resourceGroups/rg-astar-dev \
  --sdk-auth
```

```bash
curl -sL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor | sudo tee /etc/apt/trusted.gpg.d/microsoft.gpg > /dev/null

echo "deb [arch=amd64] https://packages.microsoft.com/repos/azure-cli/ $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/azure-cli.list

sudo apt-get update && sudo apt-get install azure-cli -y

az version
```

# Replace "jammy" with whatever the above command returned
echo "deb [arch=amd64] https://packages.microsoft.com/repos/azure-cli/ noble main" | sudo tee /etc/apt/sources.list.d/azure-cli.list

sudo apt-get update && sudo apt-get install azure-cli -y
az version

Check if the install actually succeeded and where the binary is:


dpkg -l azure-cli
find /usr /opt /home -name "az" 2>/dev/null
Paste the output. In the meantime, try pip as a fallback — it works on any distro:


pip3 install azure-cli --break-system-packages
az version
If pip3 isn't available:


sudo apt-get install python3-pip -y
pip3 install azure-cli --break-system-packages
