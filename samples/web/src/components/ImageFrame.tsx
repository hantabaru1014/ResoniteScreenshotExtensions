import { AspectRatio, Button } from "@/components/ui";
import { useState, useRef, DragEvent, ChangeEvent } from "react";

function ImageFrame({
  onImageUpload,
  onImageReset,
}: {
  onImageUpload?: (file: File) => void;
  onImageReset?: () => void;
}) {
  const [imageUrl, setImageUrl] = useState("");
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleUploadImage = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      changeImage(file);
    }
  };

  const handleDragOver = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    setIsDragging(false);

    const file = event.dataTransfer.files?.[0];
    if (file && file.type.startsWith("image/")) {
      changeImage(file);
    }
  };

  const changeImage = (file: File) => {
    onImageUpload?.(file);

    const reader = new FileReader();
    reader.onload = (e) => {
      setImageUrl(e.target?.result as string);
    };
    reader.readAsDataURL(file);
  };

  const handleButtonClick = () => {
    fileInputRef.current?.click();
  };

  const handleReset = () => {
    onImageReset?.();

    setImageUrl("");
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  return (
    <div>
      <AspectRatio
        ratio={16 / 9}
        className={`border-2 rounded-md p-2 ${isDragging ? "border-blue-500 bg-blue-50" : "border-gray-300"}`}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
      >
        {imageUrl ? (
          <div className="relative">
            <img
              src={imageUrl}
              alt="Selected image"
              className="w-full h-full object-contain"
            />
            <Button
              className="absolute top-2 right-2 bg-red-500 hover:bg-red-700 text-white"
              onClick={handleReset}
            >
              x
            </Button>
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center h-full">
            <input
              type="file"
              accept="image/*"
              onChange={handleUploadImage}
              className="hidden"
              ref={fileInputRef}
            />
            <p className="mb-4 text-gray-500">
              Drag and drop an image here, or click the button to select
            </p>
            <Button onClick={handleButtonClick}>Select Image</Button>
          </div>
        )}
      </AspectRatio>
    </div>
  );
}

export { ImageFrame };
