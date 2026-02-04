"use client"

import { useState } from "react"
import { MetadataPanel } from "@/components/parse-match/metadata-panel"
import { FileListPanel } from "@/components/parse-match/file-list-panel"
import { PreviewControlPanel } from "@/components/parse-match/preview-control-panel"
import { Button } from "@/components/ui/button"
import { ArrowLeft, Play } from "lucide-react"
import Link from "next/link"

export interface MediaFile {
  id: string
  name: string
  type: "video" | "image"
}

export interface SeriesInfo {
  title: string
  originalTitle: string
  year: string
  studio: string
  director: string
  actors: string
  tags: string
}

export interface EpisodeInfo {
  episodeNumber: string
  episodeTitle: string
  episodeDescription: string
  airYear: string
  tags: string
}

const selectedFiles: MediaFile[] = [
  { id: "1", name: "vacation_video.mp4", type: "video" },
  { id: "2", name: "sunset_photo.jpg", type: "image" },
  { id: "3", name: "interview_clip.mp4", type: "video" },
  { id: "4", name: "banner_design.png", type: "image" },
]

const initialEpisodes: EpisodeInfo[] = [
  { episodeNumber: "1", episodeTitle: "", episodeDescription: "", airYear: "", tags: "" },
  { episodeNumber: "2", episodeTitle: "", episodeDescription: "", airYear: "", tags: "" },
  { episodeNumber: "3", episodeTitle: "", episodeDescription: "", airYear: "", tags: "" },
]

export default function ParseMatchPage() {
  const [files] = useState<MediaFile[]>(selectedFiles)
  const [selectedFile, setSelectedFile] = useState<MediaFile | null>(selectedFiles[0])
  
  const [seriesInfo, setSeriesInfo] = useState<SeriesInfo>({
    title: "",
    originalTitle: "",
    year: "",
    studio: "",
    director: "",
    actors: "",
    tags: "",
  })
  
  const [episodes, setEpisodes] = useState<EpisodeInfo[]>(initialEpisodes)
  const [currentEpisodeIndex, setCurrentEpisodeIndex] = useState(0)
  
  const [episodeAssignment, setEpisodeAssignment] = useState<Record<string, string>>({})
  const [imageType, setImageType] = useState<Record<string, "poster" | "fanart">>({})

  const handleSeriesInfoChange = (field: keyof SeriesInfo, value: string) => {
    setSeriesInfo(prev => ({ ...prev, [field]: value }))
  }

  const handleEpisodeInfoChange = (field: keyof EpisodeInfo, value: string) => {
    setEpisodes(prev => prev.map((ep, idx) => 
      idx === currentEpisodeIndex ? { ...ep, [field]: value } : ep
    ))
  }

  const handleEpisodeAssignment = (fileId: string, episodeNumber: string) => {
    setEpisodeAssignment(prev => ({ ...prev, [fileId]: episodeNumber }))
  }

  const handleImageTypeChange = (fileId: string, type: "poster" | "fanart") => {
    setImageType(prev => ({ ...prev, [fileId]: type }))
  }

  const handleStartProcessing = () => {
    console.log("Processing started with:", {
      seriesInfo,
      episodes,
      episodeAssignment,
      imageType,
    })
  }

  return (
    <div className="flex flex-col h-screen bg-background">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-border bg-card">
        <div className="flex items-center gap-3">
          <Link href="/">
            <Button variant="ghost" size="sm" className="gap-2">
              <ArrowLeft className="h-4 w-4" />
              Back
            </Button>
          </Link>
          <div className="h-5 w-px bg-border" />
          <h1 className="text-base font-semibold text-foreground">Parse & Match</h1>
        </div>
        <Button onClick={handleStartProcessing} className="gap-2">
          <Play className="h-4 w-4" />
          Start Processing
        </Button>
      </div>

      {/* Three Column Layout */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left Column - Metadata Panel */}
        <MetadataPanel
          seriesInfo={seriesInfo}
          episodes={episodes}
          currentEpisodeIndex={currentEpisodeIndex}
          onSeriesInfoChange={handleSeriesInfoChange}
          onEpisodeInfoChange={handleEpisodeInfoChange}
          onEpisodeSelect={setCurrentEpisodeIndex}
        />

        {/* Middle Column - File List */}
        <FileListPanel
          files={files}
          selectedFile={selectedFile}
          onSelectFile={setSelectedFile}
        />

        {/* Right Column - Preview & Controls */}
        <PreviewControlPanel
          selectedFile={selectedFile}
          episodes={episodes}
          episodeAssignment={episodeAssignment}
          imageType={imageType}
          onEpisodeAssignment={handleEpisodeAssignment}
          onImageTypeChange={handleImageTypeChange}
        />
      </div>
    </div>
  )
}
