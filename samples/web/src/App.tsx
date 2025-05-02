import { ImageFrame } from "@/components/ImageFrame";
import { MetadataDisplayPanel } from "@/components/MetadataDisplayPanel";
import { loadXMPMetadata } from "@/lib/xmp";
import { useState } from "react";
import { Metadata } from "@/lib/metadata";
import { toast } from "sonner";
import {
  ResizableHandle,
  ResizablePanel,
  ResizablePanelGroup,
  Toaster,
} from "@/components/ui";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const queryClient = new QueryClient();

function Viewer() {
  const [metadata, setMetadata] = useState<Metadata | undefined>();

  const handleImageUpload = async (file: File) => {
    try {
      const data = await loadXMPMetadata(file);
      setMetadata(data ?? undefined);

      console.log("Metadata loaded:", data);
    } catch (error) {
      if (error instanceof Error) {
        console.error("Error loading metadata:", error.message);
        toast.error("Error: " + error.message);
      }
    }
  };

  return (
    <>
      <header className="bg-gray-900 text-white p-4 flex justify-between">
        <h1 className="font-bold">
          ResoniteScreenshotExtensions Metadata Viewer
        </h1>
        <a
          href="https://github.com/hantabaru1014/ResoniteScreenshotExtensions"
          target="_blank"
          className="hover:underline"
        >
          Github
        </a>
      </header>
      <main className="p-8">
        <p className="mb-6 text-gray-800">
          You can check the metadata embedded in screenshots saved with
          ResoniteScreenshotExtensions.
          <br />
          Your image data is processed in your browser and is not sent to any
          server.
        </p>
        <ResizablePanelGroup direction="horizontal">
          <ResizablePanel defaultSize={60} className="pr-2">
            <ImageFrame
              onImageUpload={handleImageUpload}
              onImageReset={() => setMetadata(undefined)}
            />
          </ResizablePanel>

          <ResizableHandle />

          <ResizablePanel defaultSize={40} className="pl-2">
            <MetadataDisplayPanel value={metadata} />
          </ResizablePanel>
        </ResizablePanelGroup>
        <Toaster position="bottom-left" />
      </main>
    </>
  );
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Viewer />
    </QueryClientProvider>
  );
}

export default App;
