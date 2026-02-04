"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { ActionBar } from "@/components/action-bar"
import { FileList } from "@/components/file-list"
import { PreviewPanel } from "@/components/preview-panel"

export interface MediaFile {
  id: string
  name: string
  type: "video" | "image"
  checked: boolean
  thumbnail?: string
}

const initialFiles: MediaFile[] = [
  { id: "1", name: "vacation_video.mp4", type: "video", checked: false },
  { id: "2", name: "sunset_photo.jpg", type: "image", checked: true },
  { id: "3", name: "presentation.mp4", type: "video", checked: false },
  { id: "4", name: "screenshot.png", type: "image", checked: false },
  { id: "5", name: "interview_clip.mp4", type: "video", checked: true },
  { id: "6", name: "product_shot.jpg", type: "image", checked: false },
  { id: "7", name: "tutorial.mp4", type: "video", checked: false },
  { id: "8", name: "banner_design.png", type: "image", checked: true },
  { id: "9", name: "demo_reel.mp4", type: "video", checked: false },
  { id: "10", name: "logo_final.png", type: "image", checked: false },
]

export default function MediaManager() {
  const router = useRouter()
  const [files, setFiles] = useState<MediaFile[]>(initialFiles)
  const [selectedFile, setSelectedFile] = useState<MediaFile | null>(initialFiles[1])
  const [folderPath, setFolderPath] = useState<string | null>(null)

  const handleToggleCheck = (id: string) => {
    setFiles(files.map(file => 
      file.id === id ? { ...file, checked: !file.checked } : file
    ))
  }

  const handleSelectFile = (file: MediaFile) => {
    setSelectedFile(file)
  }

  const handleSelectFolder = () => {
    setFolderPath("C:\\Users\\Documents\\Media Files")
  }

  const handleParseGenerate = () => {
    const checkedFiles = files.filter(f => f.checked)
    if (checkedFiles.length > 0) {
      router.push("/parse-match")
    }
  }

  return (
    <div className="flex flex-col h-screen bg-background">
      <ActionBar 
        onSelectFolder={handleSelectFolder}
        onParseGenerate={handleParseGenerate}
        folderPath={folderPath}
      />
      <div className="flex flex-1 overflow-hidden">
        <FileList 
          files={files}
          selectedFile={selectedFile}
          onToggleCheck={handleToggleCheck}
          onSelectFile={handleSelectFile}
        />
        <PreviewPanel selectedFile={selectedFile} />
      </div>
    </div>
  )
}
