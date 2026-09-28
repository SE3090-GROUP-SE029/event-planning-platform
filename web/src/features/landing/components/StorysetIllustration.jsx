import { Box } from '@mui/material';
import { storysetIllustrations } from './storysetIllustrations';

export default function StorysetIllustration({ name, alt, sx = {} }) {
  const illustration = storysetIllustrations[name];
  if (!illustration) {
    throw new Error(`Unknown Storyset illustration: ${name}`);
  }

  return (
    <Box
      component="img"
      src={illustration.src}
      alt={alt}
      loading="lazy"
      sx={{
        display: 'block',
        maxWidth: '100%',
        height: 'auto',
        objectFit: 'contain',
        ...sx,
      }}
    />
  );
}
