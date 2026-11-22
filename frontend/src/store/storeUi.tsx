import { useState } from 'react'
import { Box, Typography } from '@mui/material'
import ImageNotSupportedOutlinedIcon from '@mui/icons-material/ImageNotSupportedOutlined'
import { mediaUrl } from '../api/client'

export const storeColors = {
  forest: '#0B6E4F',
  leaf: '#14965A',
  citrus: '#D4E157',
  coral: '#E85D04',
  cream: '#F4F7F2',
  ink: '#12241C',
  mist: '#E8F0EA',
}

export function StoreProductImage({
  src,
  alt,
  className = '',
  height,
}: {
  src?: string | null
  alt: string
  className?: string
  height?: number | string
}) {
  const [broken, setBroken] = useState(false)
  const url = mediaUrl(src)

  if (!url || broken) {
    return (
      <Box
        className={`flex flex-col items-center justify-center gap-1 bg-[#E8F0EA] text-[#5C7A68] ${className}`}
        sx={{ height: height ?? '100%', minHeight: height || 160 }}
      >
        <ImageNotSupportedOutlinedIcon />
        <Typography variant="caption">Chưa có ảnh</Typography>
      </Box>
    )
  }

  return (
    <Box
      component="img"
      src={url}
      alt={alt}
      className={`object-cover ${className}`}
      sx={{ height: height ?? '100%', width: '100%', display: 'block' }}
      onError={() => setBroken(true)}
    />
  )
}
