import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function resolveResdbUrl(url: string | undefined) {
  if (!url) return undefined;
  const match = url.match(/resdb:\/\/\/([^.]+)/);
  if (!match || match.length < 2) return undefined;

  return `https://assets.resonite.com/${match[1]}`;
}
