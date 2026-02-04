"use client"

import { Video, ImageIcon, Play, Pause, Volume2, Maximize2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Slider } from "@/components/ui/slider"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Label } from "@/components/ui/label"
import { useState } from "react"
import type { MediaFile, EpisodeInfo } from "@/app/parse/page"

interface ParsePreviewPanelProps {
  selectedFile: MediaFile | null
  episodes: EpisodeInfo[]
  onEpisodeChange: (fileId: string, episodeNumber: number) => void
  onImageTypeChange: (fileId: string, imageType: "poster" | "fanart") => void
}

export function ParsePreviewPanel({
  selectedFile,
  episodes,
  onEpisodeChange,
  onImageTypeChange,
}: ParsePreviewPanelProps) {
  const [isPlaying, setIsPlaying] = useState(false)
  const [progress, setProgress] = useState([35])

  if (!selectedFile) {
    return (
      <main className="flex-1 flex items-center justify-center bg-muted/30">
        <div className="text-center text-muted-foreground">
          <div className="w-16 h-16 rounded-full bg-muted flex items-center justify-center mx-auto mb-4">
            <ImageIcon className="w-8 h-8" />
          </div>
          <p className="text-sm">Select a file to preview</p>
        </div>
      </main>
    )
  }

  return (
    <main className="flex-1 flex flex-col bg-muted/30 overflow-hidden">
      {/* Preview Area */}
      <div className="flex-1 flex items-center justify-center p-6">
        {selectedFile.type === "video" ? (
          <VideoPreview
            fileName={selectedFile.name}
            isPlaying={isPlaying}
            progress={progress}
            onTogglePlay={() => setIsPlaying(!isPlaying)}
            onProgressChange={setProgress}
          />
        ) : (
          <ImagePreview fileName={selectedFile.name} imageType={selectedFile.imageType} />
        )}
      </div>

      {/* Control Area */}
      <div className="px-6 py-4 border-t border-border bg-card">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className={`w-8 h-8 rounded flex items-center justify-center ${
              selectedFile.type === "video" ? "bg-blue-100 text-blue-600" : "bg-emerald-100 text-emerald-600"
            }`}>
              {selectedFile.type === "video" ? (
                <Video className="w-4 h-4" />
              ) : (
                <ImageIcon className="w-4 h-4" />
              )}
            </div>
            <div>
              <p className="text-sm font-medium text-foreground">{selectedFile.name}</p>
              <p className="text-xs text-muted-foreground">
                {selectedFile.type === "video" ? "Video File" : "Image File"}
              </p>
            </div>
          </div>

          {/* Assignment Controls */}
          <div className="flex items-center gap-4">
            {selectedFile.type === "video" ? (
              <div className="flex items-center gap-2">
                <Label className="text-xs text-muted-foreground">Episode:</Label>
                <Select
                  value={selectedFile.episodeNumber?.toString() || ""}
                  onValueChange={(v) => onEpisodeChange(selectedFile.id, parseInt(v))}
                >
                  <SelectTrigger className="w-40 h-8 text-sm">
                    <SelectValue placeholder="Select episode" />
                  </SelectTrigger>
                  <SelectContent>
                    {episodes.map((ep) => (
                      <SelectItem key={ep.episodeNumber} value={ep.episodeNumber}>
                        Episode {ep.episodeNumber}{ep.episodeTitle ? ` - ${ep.episodeTitle}` : ""}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <Label className="text-xs text-muted-foreground">Type:</Label>
                <Select
                  value={selectedFile.imageType || "poster"}
                  onValueChange={(v) => onImageTypeChange(selectedFile.id, v as "poster" | "fanart")}
                >
                  <SelectTrigger className="w-32 h-8 text-sm">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="poster">Poster</SelectItem>
                    <SelectItem value="fanart">Fanart</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            )}
          </div>
        </div>
      </div>
    </main>
  )
}

interface VideoPreviewProps {
  fileName: string
  isPlaying: boolean
  progress: number[]
  onTogglePlay: () => void
  onProgressChange: (value: number[]) => void
}

function VideoPreview({ fileName, isPlaying, progress, onTogglePlay, onProgressChange }: VideoPreviewProps) {
  return (
    <div className="w-full max-w-2xl">
      <div className="relative aspect-video bg-foreground/5 rounded-lg overflow-hidden border border-border">
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="text-center">
            <div className="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center mx-auto mb-3">
              <Video className="w-8 h-8 text-primary" />
            </div>
            <p className="text-sm text-muted-foreground">{fileName}</p>
          </div>
        </div>
        <button
          onClick={onTogglePlay}
          className="absolute inset-0 flex items-center justify-center bg-foreground/0 hover:bg-foreground/5 transition-colors"
        >
          <div className="w-14 h-14 rounded-full bg-primary/90 flex items-center justify-center shadow-lg">
            {isPlaying ? (
              <Pause className="w-5 h-5 text-primary-foreground" />
            ) : (
              <Play className="w-5 h-5 text-primary-foreground ml-0.5" />
            )}
          </div>
        </button>
      </div>
      <div className="mt-3 flex items-center gap-3">
        <Button
          variant="ghost"
          size="icon"
          onClick={onTogglePlay}
          className="h-7 w-7"
        >
          {isPlaying ? (
            <Pause className="w-3.5 h-3.5" />
          ) : (
            <Play className="w-3.5 h-3.5" />
          )}
        </Button>
        <span className="text-xs text-muted-foreground w-8">0:{String(Math.floor(progress[0] * 1.54)).padStart(2, '0')}</span>
        <Slider
          value={progress}
          onValueChange={onProgressChange}
          max={100}
          step={1}
          className="flex-1"
        />
        <span className="text-xs text-muted-foreground w-8">2:34</span>
        <Button variant="ghost" size="icon" className="h-7 w-7">
          <Volume2 className="w-3.5 h-3.5" />
        </Button>
        <Button variant="ghost" size="icon" className="h-7 w-7">
          <Maximize2 className="w-3.5 h-3.5" />
        </Button>
      </div>
    </div>
  )
}

interface ImagePreviewProps {
  fileName: string
  imageType?: "poster" | "fanart"
}

function ImagePreview({ fileName, imageType }: ImagePreviewProps) {
  const isPoster = imageType === "poster"

  return (
    <div className="w-full max-w-2xl flex flex-col items-center">
      <div className={`relative ${isPoster ? "aspect-[2/3] max-w-xs" : "aspect-video w-full"} bg-foreground/5 rounded-lg overflow-hidden border border-border`}>
        <div className="absolute inset-0 flex items-center justify-center bg-gradient-to-br from-muted to-muted/50">
          <div className="text-center">
            <div className="w-16 h-16 rounded-full bg-emerald-100 flex items-center justify-center mx-auto mb-3">
              <ImageIcon className="w-8 h-8 text-emerald-600" />
            </div>
            <p className="text-sm text-muted-foreground">{fileName}</p>
            <p className="text-xs text-muted-foreground/70 mt-1 capitalize">{imageType || "Image"}</p>
          </div>
        </div>
      </div>
      <div className="mt-3 flex items-center gap-2">
        <Button variant="outline" size="sm" className="text-xs h-7 bg-transparent">
          Zoom In
        </Button>
        <Button variant="outline" size="sm" className="text-xs h-7 bg-transparent">
          Zoom Out
        </Button>
        <Button variant="outline" size="sm" className="text-xs h-7 gap-1.5 bg-transparent">
          <Maximize2 className="w-3 h-3" />
          Fullscreen
        </Button>
      </div>
    </div>
  )
}
