"use client"

import { useState } from "react"
import { MetadataEditor } from "@/components/metadata-editor"
import { SelectedFileList } from "@/components/selected-file-list"
import { ParsePreviewPanel } from "@/components/parse-preview-panel"
import { Button } from "@/components/ui/button"
import { ArrowLeft, Wand2 } from "lucide-react"
import Link from "next/link"

export interface MediaFile {
  id: string
  name: string
  type: "video" | "image"
  episodeNumber?: number
  imageType?: "poster" | "fanart"
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
  { id: "1", name: "episode_01.mp4", type: "video", episodeNumber: 1 },
  { id: "2", name: "episode_02.mp4", type: "video", episodeNumber: 2 },
  { id: "3", name: "episode_03.mp4", type: "video", episodeNumber: 3 },
  { id: "4", name: "poster_main.jpg", type: "image", imageType: "poster" },
  { id: "5", name: "fanart_01.jpg", type: "image", imageType: "fanart" },
  { id: "6", name: "fanart_02.jpg", type: "image", imageType: "fanart" },
]

export default function ParseMatchPage() {
  const [files, setFiles] = useState<MediaFile[]>(selectedFiles)
  const [selectedFile, setSelectedFile] = useState<MediaFile | null>(selectedFiles[0])
  const [metadataText, setMetadataText] = useState("")
  const [isParsed, setIsParsed] = useState(false)

  const [seriesInfo, setSeriesInfo] = useState<SeriesInfo>({
    title: "",
    originalTitle: "",
    year: "",
    studio: "",
    director: "",
    actors: "",
    tags: "",
  })

  const [episodes, setEpisodes] = useState<EpisodeInfo[]>([
    { episodeNumber: "1", episodeTitle: "", episodeDescription: "", airYear: "", tags: "" },
    { episodeNumber: "2", episodeTitle: "", episodeDescription: "", airYear: "", tags: "" },
    { episodeNumber: "3", episodeTitle: "", episodeDescription: "", airYear: "", tags: "" },
  ])

  const [currentEpisodeIndex, setCurrentEpisodeIndex] = useState(0)

  const handleParse = () => {
    // Simulate parsing metadata
    setSeriesInfo({
      title: "The Silent Valley",
      originalTitle: "Das Stille Tal",
      year: "2024",
      studio: "Northern Lights Productions",
      director: "Anna Schmidt",
      actors: "John Doe, Jane Smith, Michael Johnson",
      tags: "Drama, Mystery, Thriller",
    })
    setEpisodes([
      { episodeNumber: "1", episodeTitle: "The Beginning", episodeDescription: "A mysterious letter arrives at the old mansion.", airYear: "2024", tags: "Pilot, Mystery" },
      { episodeNumber: "2", episodeTitle: "Hidden Secrets", episodeDescription: "Sarah discovers a hidden room in the basement.", airYear: "2024", tags: "Drama, Suspense" },
      { episodeNumber: "3", episodeTitle: "The Truth Revealed", episodeDescription: "The identity of the stranger is finally revealed.", airYear: "2024", tags: "Climax, Mystery" },
    ])
    setIsParsed(true)
  }

  const handleSeriesChange = (field: keyof SeriesInfo, value: string) => {
    setSeriesInfo(prev => ({ ...prev, [field]: value }))
  }

  const handleEpisodeChange = (field: keyof EpisodeInfo, value: string) => {
    setEpisodes(prev => prev.map((ep, i) => 
      i === currentEpisodeIndex ? { ...ep, [field]: value } : ep
    ))
  }

  const handleFileEpisodeChange = (fileId: string, episodeNumber: number) => {
    setFiles(prev => prev.map(f => 
      f.id === fileId ? { ...f, episodeNumber } : f
    ))
  }

  const handleFileImageTypeChange = (fileId: string, imageType: "poster" | "fanart") => {
    setFiles(prev => prev.map(f => 
      f.id === fileId ? { ...f, imageType } : f
    ))
  }

  const handleStartProcessing = () => {
    console.log("Starting processing with:", { seriesInfo, episodes, files })
  }

  return (
    <div className="flex flex-col h-screen bg-background">
      {/* Header */}
      <header className="flex items-center justify-between px-4 py-3 border-b border-border bg-card">
        <div className="flex items-center gap-4">
          <Link href="/">
            <Button variant="ghost" size="sm" className="gap-2">
              <ArrowLeft className="w-4 h-4" />
              Back
            </Button>
          </Link>
          <div className="h-5 w-px bg-border" />
          <h1 className="text-sm font-semibold text-foreground">Parse & Match</h1>
        </div>
        <Button onClick={handleStartProcessing} className="gap-2">
          <Wand2 className="w-4 h-4" />
          Start Processing
        </Button>
      </header>

      {/* Three Column Layout */}
      <div className="flex flex-1 overflow-hidden">
        {/* Left Column - Metadata Editor */}
        <MetadataEditor
          metadataText={metadataText}
          onMetadataTextChange={setMetadataText}
          onParse={handleParse}
          isParsed={isParsed}
          seriesInfo={seriesInfo}
          onSeriesChange={handleSeriesChange}
          episodes={episodes}
          currentEpisodeIndex={currentEpisodeIndex}
          onCurrentEpisodeChange={setCurrentEpisodeIndex}
          onEpisodeChange={handleEpisodeChange}
        />

        {/* Middle Column - Selected Files */}
        <SelectedFileList
          files={files}
          selectedFile={selectedFile}
          onSelectFile={setSelectedFile}
        />

        {/* Right Column - Preview */}
        <ParsePreviewPanel
          selectedFile={selectedFile}
          episodes={episodes}
          onEpisodeChange={handleFileEpisodeChange}
          onImageTypeChange={handleFileImageTypeChange}
        />
      </div>
    </div>
  )
}
