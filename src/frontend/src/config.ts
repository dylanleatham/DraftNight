function getBasePath(): string {
  const hostname = window.location.hostname;
  if (hostname.includes('draftnight.app')) return '/';
  if (hostname.includes('knucklehead.dev')) return '/draftnight/';
  return '/'; // localhost
}

export const basePath = getBasePath();
export const routerBasename = basePath.replace(/\/+$/, '') || '/';
export const apiBaseUrl = routerBasename === '/' ? '' : routerBasename;
