"use client"

import { useState } from "react"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Slider } from "@/components/ui/slider"
import { Button } from "@/components/ui/button"
import {
  Play,
  Pause,
  Volume2,
  Maximize,
  SkipBack,
  SkipForward,
  ImageIcon,
  Film,
  ZoomIn,
  ZoomOut,
  RotateCw,
} from "lucide-react"
import type { MediaFile, EpisodeInfo } from "@/app/parse-match/page"

interface PreviewControlPanelProps {
  selectedFile: MediaFile | null
  episodes: EpisodeInfo[]
  episodeAssignment: Record<string, string>
  imageType: Record<string, "poster" | "fanart">
  onEpisodeAssignment: (fileId: string, episodeNumber: string) => void
  onImageTypeChange: (fileId: string, type: "poster" | "fanart") => void
}

export function PreviewControlPanel({
  selectedFile,
  episodes,
  episodeAssignment,
  imageType,
  onEpisodeAssignment,
  onImageTypeChange,
}: PreviewControlPanelProps) {
  const [isPlaying, setIsPlaying] = useState(false)
  const [progress, setProgress] = useState([30])
  const [volume, setVolume] = useState([75])

  if (!selectedFile) {
    return (
      <div className="flex-1 bg-muted/30 flex items-center justify-center">
        <div className="text-center text-muted-foreground">
          <ImageIcon className="h-12 w-12 mx-auto mb-3 opacity-50" />
          <p className="text-sm">Select a file to preview</p>
        </div>
      </div>
    )
  }

  return (
    <div className="flex-1 bg-card flex flex-col overflow-hidden">
      {/* Preview Area */}
      <div className="flex-1 bg-muted/50 flex items-center justify-center p-4">
        <div className="relative w-full max-w-2xl aspect-video bg-secondary rounded-lg overflow-hidden flex items-center justify-center">
          {selectedFile.type === "video" ? (
            <>
              <div className="absolute inset-0 bg-gradient-to-br from-muted to-secondary" />
              <div className="relative z-10 text-center">
                <Film className="h-16 w-16 mx-auto mb-3 text-muted-foreground" />
                <p className="text-sm text-muted-foreground">{selectedFile.name}</p>
                <p className="text-xs text-muted-foreground mt-1">Video Preview</p>
              </div>
            </>
          ) : (
            <>
              <div className="absolute inset-0 bg-gradient-to-br from-emerald-50 to-muted" />
              <div className="relative z-10 text-center">
                <ImageIcon className="h-16 w-16 mx-auto mb-3 text-emerald-600/50" />
                <p className="text-sm text-muted-foreground">{selectedFile.name}</p>
                <p className="text-xs text-muted-foreground mt-1">Image Preview</p>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Controls Area */}
      <div className="border-t border-border p-4 space-y-4">
        {selectedFile.type === "video" ? (
          <>
            {/* Video Controls */}
            <div className="space-y-3">
              <div className="flex items-center gap-2">
                <span className="text-xs text-muted-foreground w-10">0:45</span>
                <Slider
                  value={progress}
                  onValueChange={setProgress}
                  max={100}
                  step={1}
                  className="flex-1"
                />
                <span className="text-xs text-muted-foreground w-10">2:30</span>
              </div>
              
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-1">
                  <Button variant="ghost" size="icon" className="h-8 w-8">
                    <SkipBack className="h-4 w-4" />
                  </Button>
                  <Button 
                    variant="ghost" 
                    size="icon" 
                    className="h-10 w-10"
                    onClick={() => setIsPlaying(!isPlaying)}
                  >
                    {isPlaying ? (
                      <Pause className="h-5 w-5" />
                    ) : (
                      <Play className="h-5 w-5" />
                    )}
                  </Button>
                  <Button variant="ghost" size="icon" className="h-8 w-8">
                    <SkipForward className="h-4 w-4" />
                  </Button>
                </div>
                
                <div className="flex items-center gap-2">
                  <Volume2 className="h-4 w-4 text-muted-foreground" />
                  <Slider
                    value={volume}
                    onValueChange={setVolume}
                    max={100}
                    step={1}
                    className="w-24"
                  />
                  <Button variant="ghost" size="icon" className="h-8 w-8">
                    <Maximize className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            </div>

            {/* Episode Assignment */}
            <div className="pt-2 border-t border-border">
              <Label className="text-xs text-muted-foreground mb-2 block">
                Assign to Episode
              </Label>
              <Select
                value={episodeAssignment[selectedFile.id] || ""}
                onValueChange={(v) => onEpisodeAssignment(selectedFile.id, v)}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select Episode Number" />
                </SelectTrigger>
                <SelectContent>
                  {episodes.map((ep) => (
                    <SelectItem key={ep.episodeNumber} value={ep.episodeNumber}>
                      Episode {ep.episodeNumber}
                      {ep.episodeTitle && ` - ${ep.episodeTitle}`}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </>
        ) : (
          <>
            {/* Image Controls */}
            <div className="flex items-center justify-center gap-2">
              <Button variant="outline" size="icon" className="h-8 w-8 bg-transparent">
                <ZoomOut className="h-4 w-4" />
              </Button>
              <Button variant="outline" size="icon" className="h-8 w-8 bg-transparent">
                <ZoomIn className="h-4 w-4" />
              </Button>
              <Button variant="outline" size="icon" className="h-8 w-8 bg-transparent">
                <RotateCw className="h-4 w-4" />
              </Button>
              <Button variant="outline" size="icon" className="h-8 w-8 bg-transparent">
                <Maximize className="h-4 w-4" />
              </Button>
            </div>

            {/* Image Type Assignment */}
            <div className="pt-2 border-t border-border">
              <Label className="text-xs text-muted-foreground mb-2 block">
                Image Type
              </Label>
              <Select
                value={imageType[selectedFile.id] || ""}
                onValueChange={(v) => onImageTypeChange(selectedFile.id, v as "poster" | "fanart")}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select Type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="poster">Poster</SelectItem>
                  <SelectItem value="fanart">Fanart</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
