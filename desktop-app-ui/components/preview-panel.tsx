"use client"

import { Video, ImageIcon, Play, Pause, Volume2, Maximize2 } from "lucide-react"
import type { MediaFile } from "@/app/page"
import { Button } from "@/components/ui/button"
import { Slider } from "@/components/ui/slider"
import { useState } from "react"

interface PreviewPanelProps {
  selectedFile: MediaFile | null
}

export function PreviewPanel({ selectedFile }: PreviewPanelProps) {
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
          <ImagePreview fileName={selectedFile.name} />
        )}
      </div>
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
                {selectedFile.type === "video" ? "Video File • MP4" : "Image File • " + selectedFile.name.split('.').pop()?.toUpperCase()}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            {selectedFile.type === "video" ? (
              <>
                <span>1920 × 1080</span>
                <span>•</span>
                <span>2:34</span>
                <span>•</span>
                <span>24.5 MB</span>
              </>
            ) : (
              <>
                <span>1920 × 1080</span>
                <span>•</span>
                <span>2.4 MB</span>
              </>
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
    <div className="w-full max-w-3xl">
      <div className="relative aspect-video bg-foreground/5 rounded-lg overflow-hidden border border-border">
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="text-center">
            <div className="w-20 h-20 rounded-full bg-primary/10 flex items-center justify-center mx-auto mb-3">
              <Video className="w-10 h-10 text-primary" />
            </div>
            <p className="text-sm text-muted-foreground">{fileName}</p>
          </div>
        </div>
        <button 
          onClick={onTogglePlay}
          className="absolute inset-0 flex items-center justify-center bg-foreground/0 hover:bg-foreground/5 transition-colors"
        >
          <div className="w-16 h-16 rounded-full bg-primary/90 flex items-center justify-center shadow-lg">
            {isPlaying ? (
              <Pause className="w-6 h-6 text-primary-foreground" />
            ) : (
              <Play className="w-6 h-6 text-primary-foreground ml-1" />
            )}
          </div>
        </button>
      </div>
      <div className="mt-4 flex items-center gap-4">
        <Button 
          variant="ghost" 
          size="icon"
          onClick={onTogglePlay}
          className="h-8 w-8"
        >
          {isPlaying ? (
            <Pause className="w-4 h-4" />
          ) : (
            <Play className="w-4 h-4" />
          )}
        </Button>
        <span className="text-xs text-muted-foreground w-10">0:{String(Math.floor(progress[0] * 1.54)).padStart(2, '0')}</span>
        <Slider 
          value={progress}
          onValueChange={onProgressChange}
          max={100}
          step={1}
          className="flex-1"
        />
        <span className="text-xs text-muted-foreground w-10">2:34</span>
        <Button variant="ghost" size="icon" className="h-8 w-8">
          <Volume2 className="w-4 h-4" />
        </Button>
        <Button variant="ghost" size="icon" className="h-8 w-8">
          <Maximize2 className="w-4 h-4" />
        </Button>
      </div>
    </div>
  )
}

interface ImagePreviewProps {
  fileName: string
}

function ImagePreview({ fileName }: ImagePreviewProps) {
  return (
    <div className="w-full max-w-3xl">
      <div className="relative aspect-video bg-foreground/5 rounded-lg overflow-hidden border border-border">
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="w-full h-full bg-gradient-to-br from-muted to-muted/50 flex items-center justify-center">
            <div className="text-center">
              <div className="w-20 h-20 rounded-full bg-emerald-100 flex items-center justify-center mx-auto mb-3">
                <ImageIcon className="w-10 h-10 text-emerald-600" />
              </div>
              <p className="text-sm text-muted-foreground">{fileName}</p>
            </div>
          </div>
        </div>
      </div>
      <div className="mt-4 flex items-center justify-center gap-2">
        <Button variant="outline" size="sm">
          Zoom In
        </Button>
        <Button variant="outline" size="sm">
          Zoom Out
        </Button>
        <Button variant="outline" size="sm">
          <Maximize2 className="w-4 h-4 mr-2" />
          Fullscreen
        </Button>
      </div>
    </div>
  )
}
