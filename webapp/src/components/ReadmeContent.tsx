"use client";

import { useEffect } from "react";
import type { ReactNode } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

type MermaidLibrary = {
  initialize?: (config: { startOnLoad: boolean; theme: string }) => void;
  run?: () => void;
};

let mermaidLoader: Promise<typeof import("mermaid")> | null = null;
let mermaidReady = false;

async function ensureMermaid() {
  if (!mermaidLoader) {
    mermaidLoader = import("mermaid");
  }
  const importedModule = await mermaidLoader;
  const library = (importedModule as { default?: unknown }).default ?? importedModule;
  const lib = library as MermaidLibrary;

  if (!mermaidReady && typeof lib.initialize === "function") {
    lib.initialize({ startOnLoad: false, theme: "dark" });
    mermaidReady = true;
  }

  return lib;
}

type CodeProps = {
  inline?: boolean;
  className?: string;
  children?: ReactNode;
};

function MermaidBlock({ value }: { value: string }) {
  return (
    <div className="mermaid-wrapper">
      <pre className="mermaid">{value.trim()}</pre>
    </div>
  );
}

export default function ReadmeContent({ markdown }: { markdown: string }) {
  useEffect(() => {
    let active = true;
    (async () => {
      const lib = await ensureMermaid();
      if (active && typeof lib.run === "function") {
        lib.run();
      }
    })();
    return () => {
      active = false;
    };
  }, [markdown]);

  return (
    <ReactMarkdown
      remarkPlugins={[remarkGfm]}
      components={{
        code: ({ inline, className, children }: CodeProps) => {
          const value = String(children ?? "");
          const language = className?.replace("language-", "");

          if (!inline && language === "mermaid") {
            return <MermaidBlock value={value} />;
          }

          if (inline) {
            return (
              <code className="inline-code">
                {value.trim()}
              </code>
            );
          }

          return (
            <pre className="code-block">
              <code>{value.trim()}</code>
            </pre>
          );
        },
      }}
    >
      {markdown}
    </ReactMarkdown>
  );
}
